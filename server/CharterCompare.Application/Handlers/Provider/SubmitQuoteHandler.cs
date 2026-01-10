using CharterCompare.Application.MediatR;
using CharterCompare.Application.Requests.Provider;
using CharterCompare.Application.Services;
using CharterCompare.Application.Storage;
using CharterCompare.Domain.Entities;
using CharterCompare.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CharterCompare.Application.Handlers.Provider;

public class SubmitQuoteHandler : IRequestHandler<SubmitQuoteCommand, SubmitQuoteResponse>
{
    private readonly IStorage _storage;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly INotificationService _notificationService;
    private readonly ILogger<SubmitQuoteHandler> _logger;

    public SubmitQuoteHandler(
        IStorage storage, 
        IHttpContextAccessor httpContextAccessor, 
        INotificationService notificationService,
        ILogger<SubmitQuoteHandler> logger)
    {
        _storage = storage;
        _httpContextAccessor = httpContextAccessor;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<SubmitQuoteResponse> Handle(SubmitQuoteCommand request, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var providerIdClaim = httpContext?.User.FindFirst("ProviderId")?.Value;
        
        if (string.IsNullOrEmpty(providerIdClaim) || !int.TryParse(providerIdClaim, out var providerId))
        {
            return new SubmitQuoteResponse
            {
                Success = false,
                Error = "Provider not authenticated"
            };
        }

        // Get request and verify it's Open
        var charterRequest = await _storage.GetCharterRequestByIdAsync(request.RequestId, cancellationToken);
        if (charterRequest == null || charterRequest.Status != RequestStatus.Published)
        {
            return new SubmitQuoteResponse
            {
                Success = false,
                Error = "Request not found or not in valid state for quoting"
            };
        }

        // Create quote
        var quote = new Quote
        {
            ProviderId = providerId,
            CharterRequestId = request.RequestId,
            Price = request.Price,
            Currency = request.Currency,
            Notes = request.Notes,
            Status = QuoteStatus.Submitted,
            CreatedAt = DateTime.UtcNow
        };

        await _storage.CreateQuoteAsync(quote, cancellationToken);

        // Reload request with quotes to get accurate count
        charterRequest = await _storage.GetCharterRequestByIdAsync(request.RequestId, cancellationToken);
        if (charterRequest == null)
        {
            return new SubmitQuoteResponse
            {
                Success = false,
                Error = "Request not found after quote creation"
            };
        }

        var quoteCount = charterRequest.Quotes.Count;
        var isFirstQuote = quoteCount == 1;

        // Update request status if first quote
        if (isFirstQuote)
        {
            charterRequest.Status = RequestStatus.QuotesReceived;
            await _storage.UpdateCharterRequestAsync(charterRequest, cancellationToken);
        }

        await _storage.SaveChangesAsync(cancellationToken);

        // Send notifications
        try
        {
            if (isFirstQuote)
            {
                await _notificationService.NotifyFirstQuoteReceivedAsync(charterRequest, quote, cancellationToken);
            }
            else
            {
                await _notificationService.NotifyNewQuoteReceivedAsync(charterRequest, quote, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            // Log but don't fail the quote submission if notification fails
            _logger.LogError(ex, "Failed to send notification for quote {QuoteId}", quote.Id);
        }

        _logger.LogInformation("Quote submitted by provider {ProviderId} for request {RequestId}", providerId, request.RequestId);

        return new SubmitQuoteResponse
        {
            Success = true,
            QuoteId = quote.Id
        };
    }
}

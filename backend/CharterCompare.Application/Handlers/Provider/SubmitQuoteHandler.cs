using CharterCompare.Application.MediatR;
using CharterCompare.Application.Requests.Provider;
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
    private readonly ILogger<SubmitQuoteHandler> _logger;

    public SubmitQuoteHandler(IStorage storage, IHttpContextAccessor httpContextAccessor, ILogger<SubmitQuoteHandler> logger)
    {
        _storage = storage;
        _httpContextAccessor = httpContextAccessor;
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

        // Update request status if first quote
        var quoteCount = (await _storage.GetQuotesByRequestIdAsync(request.RequestId, cancellationToken)).Count;
        if (quoteCount == 1)
        {
            charterRequest.Status = RequestStatus.QuotesReceived;
            await _storage.UpdateCharterRequestAsync(charterRequest, cancellationToken);
        }

        await _storage.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Quote submitted by provider {ProviderId} for request {RequestId}", providerId, request.RequestId);

        return new SubmitQuoteResponse
        {
            Success = true,
            QuoteId = quote.Id
        };
    }
}

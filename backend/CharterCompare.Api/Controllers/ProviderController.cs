using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CharterCompare.Api.Services;
using CharterCompare.Api.Models;
using System.Security.Claims;

namespace CharterCompare.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProviderController : ControllerBase
{
    private readonly IProviderService _providerService;
    private readonly ILogger<ProviderController> _logger;

    public ProviderController(IProviderService providerService, ILogger<ProviderController> logger)
    {
        _providerService = providerService;
        _logger = logger;
    }

    private int GetProviderId()
    {
        var providerIdClaim = User.FindFirst("ProviderId")?.Value;
        if (string.IsNullOrEmpty(providerIdClaim) || !int.TryParse(providerIdClaim, out var providerId))
        {
            throw new UnauthorizedAccessException("Invalid provider ID");
        }
        return providerId;
    }

    [HttpGet("requests")]
    public async Task<ActionResult<List<CharterRequestRecord>>> GetRequests()
    {
        try
        {
            var requests = await _providerService.GetOpenRequestsAsync();
            return Ok(requests.Select(r => new
            {
                id = r.Id,
                sessionId = r.SessionId,
                requestData = r.RequestData,
                createdAt = r.CreatedAt,
                status = r.Status.ToString()
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching requests");
            return StatusCode(500, new { error = "Failed to fetch requests" });
        }
    }

    [HttpGet("requests/{id}")]
    public async Task<ActionResult> GetRequest(int id)
    {
        try
        {
            var request = await _providerService.GetRequestByIdAsync(id);
            if (request == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                id = request.Id,
                sessionId = request.SessionId,
                requestData = request.RequestData,
                createdAt = request.CreatedAt,
                status = request.Status.ToString(),
                quotes = request.Quotes?.Select(q => new
                {
                    id = q.Id,
                    providerId = q.ProviderId,
                    providerName = q.Provider?.Name,
                    price = q.Price,
                    currency = q.Currency,
                    notes = q.Notes,
                    status = q.Status.ToString(),
                    createdAt = q.CreatedAt
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching request {RequestId}", id);
            return StatusCode(500, new { error = "Failed to fetch request" });
        }
    }

    [HttpPost("requests/{requestId}/quotes")]
    public async Task<ActionResult> SubmitQuote(int requestId, [FromBody] SubmitQuoteRequest quoteRequest)
    {
        try
        {
            var providerId = GetProviderId();
            var quote = await _providerService.SubmitQuoteAsync(
                providerId,
                requestId,
                quoteRequest.Price,
                quoteRequest.Currency ?? "AUD",
                quoteRequest.Notes
            );

            return Ok(new
            {
                id = quote.Id,
                price = quote.Price,
                currency = quote.Currency,
                notes = quote.Notes,
                status = quote.Status.ToString(),
                createdAt = quote.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting quote for request {RequestId}", requestId);
            return StatusCode(500, new { error = "Failed to submit quote" });
        }
    }

    [HttpGet("quotes")]
    public async Task<ActionResult> GetMyQuotes()
    {
        try
        {
            var providerId = GetProviderId();
            var quotes = await _providerService.GetProviderQuotesAsync(providerId);

            return Ok(quotes.Select(q => new
            {
                id = q.Id,
                requestId = q.CharterRequestId,
                requestData = q.CharterRequest.RequestData,
                price = q.Price,
                currency = q.Currency,
                notes = q.Notes,
                status = q.Status.ToString(),
                createdAt = q.CreatedAt
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching quotes");
            return StatusCode(500, new { error = "Failed to fetch quotes" });
        }
    }
}

public class SubmitQuoteRequest
{
    public decimal Price { get; set; }
    public string? Currency { get; set; }
    public string? Notes { get; set; }
}

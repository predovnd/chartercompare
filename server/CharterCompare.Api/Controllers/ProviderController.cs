using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CharterCompare.Application.MediatR;
using CharterCompare.Application.Requests.Provider;
using System.Security.Claims;

namespace CharterCompare.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProviderController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProviderController> _logger;

    public ProviderController(IMediator mediator, ILogger<ProviderController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet("requests")]
    public async Task<ActionResult> GetRequests()
    {
        try
        {
            var query = new GetProviderRequestsQuery();
            var response = await _mediator.Send(query);
            return Ok(response.Requests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching requests");
            return StatusCode(500, new { error = "Failed to fetch requests" });
        }
    }

    [HttpGet("quotes")]
    public async Task<ActionResult> GetMyQuotes()
    {
        try
        {
            var query = new GetProviderQuotesQuery();
            var response = await _mediator.Send(query);
            return Ok(response.Quotes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching provider quotes");
            return StatusCode(500, new { error = "Failed to fetch quotes" });
        }
    }

    [HttpPost("requests/{requestId}/quotes")]
    public async Task<ActionResult> SubmitQuote(int requestId, [FromBody] SubmitQuoteRequest quoteRequest)
    {
        try
        {
            var command = new SubmitQuoteCommand
            {
                RequestId = requestId,
                Price = quoteRequest.Price,
                Currency = quoteRequest.Currency ?? "AUD",
                Notes = quoteRequest.Notes
            };

            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                return BadRequest(new { error = response.Error });
            }

            return Ok(new
            {
                id = response.QuoteId,
                price = quoteRequest.Price,
                currency = quoteRequest.Currency ?? "AUD",
                notes = quoteRequest.Notes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting quote for request {RequestId}", requestId);
            return StatusCode(500, new { error = "Failed to submit quote" });
        }
    }
}

public class SubmitQuoteRequest
{
    public decimal Price { get; set; }
    public string? Currency { get; set; }
    public string? Notes { get; set; }
}

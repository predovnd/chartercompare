using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CharterCompare.Api.Services;
using System.Security.Claims;

namespace CharterCompare.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RequesterController : ControllerBase
{
    private readonly IRequesterService _requesterService;
    private readonly ILogger<RequesterController> _logger;

    public RequesterController(IRequesterService requesterService, ILogger<RequesterController> logger)
    {
        _requesterService = requesterService;
        _logger = logger;
    }

    private int GetRequesterId()
    {
        var requesterIdClaim = User.FindFirst("RequesterId")?.Value;
        if (string.IsNullOrEmpty(requesterIdClaim) || !int.TryParse(requesterIdClaim, out var requesterId))
        {
            throw new UnauthorizedAccessException("Invalid requester ID");
        }
        return requesterId;
    }

    [HttpGet("requests")]
    public async Task<ActionResult> GetMyRequests()
    {
        try
        {
            var requesterId = GetRequesterId();
            var requests = await _requesterService.GetRequesterRequestsAsync(requesterId);
            
            return Ok(requests.Select(r => new
            {
                id = r.Id,
                sessionId = r.SessionId,
                requestData = r.RequestData,
                status = r.Status.ToString(),
                createdAt = r.CreatedAt,
                quoteCount = r.Quotes.Count,
                quotes = r.Quotes.Select(q => new
                {
                    id = q.Id,
                    operatorName = q.Provider.Name,
                    operatorEmail = q.Provider.Email,
                    price = q.Price,
                    currency = q.Currency,
                    notes = q.Notes,
                    status = q.Status.ToString(),
                    createdAt = q.CreatedAt
                })
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching requester requests");
            return StatusCode(500, new { error = "Failed to fetch requests" });
        }
    }

    [HttpGet("requests/{id}")]
    public async Task<ActionResult> GetRequest(int id)
    {
        try
        {
            var requesterId = GetRequesterId();
            var request = await _requesterService.GetRequestByIdAsync(id, requesterId);
            
            if (request == null)
            {
                return NotFound(new { error = "Request not found" });
            }

            return Ok(new
            {
                id = request.Id,
                sessionId = request.SessionId,
                requestData = request.RequestData,
                status = request.Status.ToString(),
                createdAt = request.CreatedAt,
                quotes = request.Quotes.Select(q => new
                {
                    id = q.Id,
                    operatorName = q.Provider.Name,
                    operatorEmail = q.Provider.Email,
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
            _logger.LogError(ex, "Error fetching request");
            return StatusCode(500, new { error = "Failed to fetch request" });
        }
    }
}

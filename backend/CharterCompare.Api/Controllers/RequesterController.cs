using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CharterCompare.Application.MediatR;
using CharterCompare.Application.Requests.Requester;

namespace CharterCompare.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RequesterController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<RequesterController> _logger;

    public RequesterController(IMediator mediator, ILogger<RequesterController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet("requests/session/{sessionId}")]
    public async Task<ActionResult> GetRequestBySessionId(string sessionId)
    {
        try
        {
            var query = new GetRequestBySessionIdQuery { SessionId = sessionId };
            var response = await _mediator.Send(query);
            
            if (response.Request == null)
            {
                return NotFound(new { error = "Request not found" });
            }
            
            return Ok(response.Request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching request by session ID");
            return StatusCode(500, new { error = "Failed to fetch request" });
        }
    }

    [HttpGet("requests")]
    public async Task<ActionResult> GetMyRequests()
    {
        try
        {
            var query = new GetRequesterRequestsQuery();
            var response = await _mediator.Send(query);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching requester requests");
            return StatusCode(500, new { error = "Failed to fetch requests" });
        }
    }
}

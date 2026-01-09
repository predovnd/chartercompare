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

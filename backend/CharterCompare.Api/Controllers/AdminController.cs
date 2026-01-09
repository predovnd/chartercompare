using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CharterCompare.Application.MediatR;
using CharterCompare.Application.Requests.Admin;
using System.Security.Claims;

namespace CharterCompare.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IMediator mediator, ILogger<AdminController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    private bool IsAdmin()
    {
        return User.FindFirst("IsAdmin")?.Value == "true";
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        if (!IsAdmin())
        {
            return Forbid("Admin access required");
        }

        var query = new GetAdminStatsQuery();
        var response = await _mediator.Send(query);

        return Ok(new
        {
            totalOperators = response.TotalOperators,
            totalRequests = response.TotalRequests,
            openRequests = response.OpenRequests,
            totalQuotes = response.TotalQuotes,
            totalRequesters = response.TotalRequesters
        });
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        if (!IsAdmin())
        {
            return Forbid("Admin access required");
        }

        var query = new GetAdminUsersQuery();
        var response = await _mediator.Send(query);

        return Ok(response.Users);
    }

    [HttpGet("requests")]
    public async Task<IActionResult> GetRequests()
    {
        if (!IsAdmin())
        {
            return Forbid("Admin access required");
        }

        var query = new GetAdminRequestsQuery();
        var response = await _mediator.Send(query);

        return Ok(response.Requests);
    }
}

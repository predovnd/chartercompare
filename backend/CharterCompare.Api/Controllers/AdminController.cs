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

        try
        {
            _logger.LogInformation("Admin users endpoint called");
            var query = new GetAdminUsersQuery();
            var response = await _mediator.Send(query);
            
            _logger.LogInformation("Returning {Count} users", response.Users?.Count ?? 0);
            return Ok(response.Users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users: {Error}", ex.Message);
            return StatusCode(500, new { error = "Failed to retrieve users", message = ex.Message });
        }
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

    [HttpPut("users/{userId}/attributes")]
    public async Task<IActionResult> UpdateUserAttributes(int userId, [FromBody] UpdateUserAttributesRequest request)
    {
        if (!IsAdmin())
        {
            return Forbid("Admin access required");
        }

        var command = new UpdateUserAttributesCommand
        {
            UserId = userId,
            Attributes = request.Attributes,
            CompanyName = request.CompanyName
        };

        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            return BadRequest(new { error = response.Error });
        }

        return Ok(new { message = "User attributes updated successfully" });
    }

    [HttpPut("users/{userId}/active")]
    public async Task<IActionResult> UpdateUserActiveStatus(int userId, [FromBody] UpdateUserActiveStatusRequest request)
    {
        if (!IsAdmin())
        {
            return Forbid("Admin access required");
        }

        var command = new UpdateUserActiveStatusCommand
        {
            UserId = userId,
            IsActive = request.IsActive
        };

        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            return BadRequest(new { error = response.Error });
        }

        return Ok(new { message = "User active status updated successfully" });
    }

    [HttpPost("operators/{operatorId}/coverage")]
    public async Task<IActionResult> ConfigureOperatorCoverage(int operatorId, [FromBody] ConfigureOperatorCoverageRequest request)
    {
        if (!IsAdmin())
        {
            return Forbid("Admin access required");
        }

        var command = new ConfigureOperatorCoverageCommand
        {
            OperatorId = operatorId,
            BaseLocationName = request.BaseLocationName,
            CoverageRadiusKm = request.CoverageRadiusKm,
            MinPassengerCapacity = request.MinPassengerCapacity,
            MaxPassengerCapacity = request.MaxPassengerCapacity
        };

        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            return BadRequest(new { error = response.Error });
        }

        return Ok(response.Coverage);
    }

    [HttpGet("operators/{operatorId}/coverage")]
    public async Task<IActionResult> GetOperatorCoverages(int operatorId)
    {
        if (!IsAdmin())
        {
            return Forbid("Admin access required");
        }

        var query = new GetOperatorCoveragesQuery
        {
            OperatorId = operatorId
        };

        var response = await _mediator.Send(query);
        // Return the first coverage (since we only allow one for now)
        return Ok(response.Coverages.FirstOrDefault());
    }

    [HttpPut("operators/coverage/{coverageId}")]
    public async Task<IActionResult> UpdateOperatorCoverage(int coverageId, [FromBody] ConfigureOperatorCoverageRequest request)
    {
        if (!IsAdmin())
        {
            return Forbid("Admin access required");
        }

        var command = new UpdateOperatorCoverageCommand
        {
            CoverageId = coverageId,
            BaseLocationName = request.BaseLocationName,
            CoverageRadiusKm = request.CoverageRadiusKm,
            MinPassengerCapacity = request.MinPassengerCapacity,
            MaxPassengerCapacity = request.MaxPassengerCapacity
        };

        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            return BadRequest(new { error = response.Error });
        }

        return Ok(response.Coverage);
    }

    [HttpDelete("operators/coverage/{coverageId}")]
    public async Task<IActionResult> DeleteOperatorCoverage(int coverageId)
    {
        if (!IsAdmin())
        {
            return Forbid("Admin access required");
        }

        var command = new DeleteOperatorCoverageCommand
        {
            CoverageId = coverageId
        };

        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            return BadRequest(new { error = response.Error });
        }

        return Ok(new { message = "Coverage deleted successfully" });
    }

    [HttpGet("geocode")]
    public async Task<IActionResult> GeocodeLocation([FromQuery] string location)
    {
        if (!IsAdmin())
        {
            return Forbid("Admin access required");
        }

        if (string.IsNullOrWhiteSpace(location))
        {
            return BadRequest(new { error = "Location parameter is required" });
        }

        try
        {
            var geocodingService = HttpContext.RequestServices.GetRequiredService<CharterCompare.Application.Services.IGeocodingService>();
            var result = await geocodingService.GeocodeAsync(location);

            if (result == null)
            {
                return NotFound(new { error = "Could not resolve location" });
            }

            return Ok(new
            {
                latitude = result.Latitude,
                longitude = result.Longitude,
                displayName = result.DisplayName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error geocoding location: {Error}", ex.Message);
            return StatusCode(500, new { error = "Failed to geocode location" });
        }
    }

    [HttpPut("requests/{requestId}/location")]
    public async Task<IActionResult> UpdateRequestLocation(int requestId, [FromBody] UpdateRequestLocationRequest request)
    {
        if (!IsAdmin())
        {
            return Forbid("Admin access required");
        }

        var command = new UpdateRequestLocationCommand
        {
            RequestId = requestId,
            LocationType = request.LocationType,
            LocationName = request.LocationName,
            Latitude = request.Latitude,
            Longitude = request.Longitude
        };

        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            return BadRequest(new { error = response.Error });
        }

        return Ok(new { message = "Location updated successfully" });
    }

    [HttpPost("requests/{requestId}/publish")]
    public async Task<IActionResult> PublishRequest(int requestId)
    {
        if (!IsAdmin())
        {
            return Forbid("Admin access required");
        }

        var command = new PublishRequestCommand
        {
            RequestId = requestId
        };

        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            return BadRequest(new { error = response.Error });
        }

        return Ok(new { message = "Request published successfully" });
    }

    [HttpPost("requests/{requestId}/withdraw")]
    public async Task<IActionResult> WithdrawRequest(int requestId)
    {
        if (!IsAdmin())
        {
            return Forbid("Admin access required");
        }

        var command = new WithdrawRequestCommand
        {
            RequestId = requestId
        };

        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            return BadRequest(new { error = response.Error });
        }

        return Ok(new { message = "Request withdrawn successfully" });
    }
}

public class UpdateUserAttributesRequest
{
    public List<Domain.Enums.UserAttributeType> Attributes { get; set; } = new();
    public string? CompanyName { get; set; } // Required when setting Business attribute
}

public class ConfigureOperatorCoverageRequest
{
    public string BaseLocationName { get; set; } = string.Empty;
    public double CoverageRadiusKm { get; set; }
    public int MinPassengerCapacity { get; set; }
    public int MaxPassengerCapacity { get; set; }
}

public class UpdateRequestLocationRequest
{
    public string LocationType { get; set; } = string.Empty; // "pickup" or "destination"
    public string LocationName { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class UpdateUserActiveStatusRequest
{
    public bool IsActive { get; set; }
}

using Microsoft.AspNetCore.Mvc;
using CharterCompare.Application.MediatR;
using CharterCompare.Application.Requests.Auth;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;

namespace CharterCompare.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IMediator mediator, IConfiguration configuration, ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("google")]
    public IActionResult GoogleLogin([FromQuery] string? userType = "operator")
    {
        var googleClientId = _configuration["Authentication:Google:ClientId"];
        if (string.IsNullOrEmpty(googleClientId))
        {
            return BadRequest(new { error = "Google OAuth is not configured. Please add ClientId and ClientSecret to appsettings.json" });
        }
        
        var redirectUrl = "http://localhost:5000/api/auth/google-callback";
        
        _logger.LogInformation("=== Google OAuth Configuration ===");
        _logger.LogInformation("User Type: {UserType}", userType);
        _logger.LogInformation("Request Scheme: {Scheme}", Request.Scheme);
        _logger.LogInformation("Request Host: {Host}", Request.Host);
        _logger.LogInformation("Using Redirect URL: {RedirectUrl}", redirectUrl);
        if (!string.IsNullOrEmpty(googleClientId))
        {
            var clientIdPreview = googleClientId.Length > 20 ? googleClientId.Substring(0, 20) + "..." : googleClientId;
            _logger.LogInformation("Google ClientId: {ClientId}", clientIdPreview);
        }
        
        var properties = new AuthenticationProperties 
        { 
            RedirectUri = redirectUrl,
            Items = { { "userType", userType ?? "operator" } }
        };
        return Challenge(properties, "Google");
    }

    [HttpGet("google-callback")]
    public IActionResult GoogleCallback()
    {
        var frontendUrl = _configuration["Frontend:Url"] ?? "http://localhost:5173";
        
        if (User.Identity?.IsAuthenticated ?? false)
        {
            _logger.LogInformation("User already authenticated, redirecting to dashboard");
            var requesterId = User.FindFirst("RequesterId")?.Value;
            if (!string.IsNullOrEmpty(requesterId))
            {
                return Redirect($"{frontendUrl}/requester/dashboard");
            }
            return Redirect($"{frontendUrl}/provider/dashboard");
        }
        
        _logger.LogWarning("Callback reached but user not authenticated. Redirecting to login.");
        return Redirect($"{frontendUrl}/provider/login?error=callback_failed");
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new { message = "Logged out successfully" });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var command = new OperatorRegisterCommand
        {
            Email = request.Email,
            Password = request.Password,
            Name = request.Name,
            CompanyName = request.CompanyName
        };

        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            return BadRequest(new { error = response.Error });
        }

        // Sign in the user
        var sessionClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, response.Operator!.Id.ToString()),
            new Claim(ClaimTypes.Email, response.Operator.Email),
            new Claim(ClaimTypes.Name, response.Operator.Name),
            new Claim("ProviderId", response.Operator.Id.ToString())
        };

        var claimsIdentity = new ClaimsIdentity(sessionClaims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity), authProperties);

        return Ok(new
        {
            message = "Registration successful",
            provider = new
            {
                id = response.Operator.Id,
                email = response.Operator.Email,
                name = response.Operator.Name,
                companyName = response.Operator.CompanyName
            }
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var command = new OperatorLoginCommand
        {
            Email = request.Email,
            Password = request.Password
        };

        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            return Unauthorized(new { error = response.Error });
        }

        // Sign in the user
        var sessionClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, response.Operator!.Id.ToString()),
            new Claim(ClaimTypes.Email, response.Operator.Email),
            new Claim(ClaimTypes.Name, response.Operator.Name),
            new Claim("ProviderId", response.Operator.Id.ToString())
        };

        if (response.Operator.IsAdmin)
        {
            sessionClaims.Add(new Claim("IsAdmin", "true"));
        }

        var claimsIdentity = new ClaimsIdentity(sessionClaims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity), authProperties);

        _logger.LogInformation("Operator logged in: {Email}", request.Email);

        return Ok(new
        {
            message = "Login successful",
            provider = new
            {
                id = response.Operator.Id,
                email = response.Operator.Email,
                name = response.Operator.Name,
                companyName = response.Operator.CompanyName
            }
        });
    }

    [HttpPost("admin/create-admin")]
    public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminRequest request)
    {
        var command = new CreateAdminCommand
        {
            Email = request.Email,
            Name = request.Name,
            Password = request.Password,
            CompanyName = request.CompanyName
        };

        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            return BadRequest(new { error = response.Error });
        }

        _logger.LogInformation("Admin created: {Email}", request.Email);

        return Ok(new
        {
            message = "Admin created successfully",
            email = response.Email,
            name = response.Name
        });
    }

    [HttpPost("admin/login")]
    public async Task<IActionResult> AdminLogin([FromBody] LoginRequest request)
    {
        var command = new OperatorLoginCommand
        {
            Email = request.Email,
            Password = request.Password
        };

        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            return Unauthorized(new { error = response.Error });
        }

        if (!response.Operator!.IsAdmin)
        {
            return Unauthorized(new { error = "Access denied. Admin privileges required." });
        }

        // Sign in the user
        var sessionClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, response.Operator.Id.ToString()),
            new Claim(ClaimTypes.Email, response.Operator.Email),
            new Claim(ClaimTypes.Name, response.Operator.Name),
            new Claim("ProviderId", response.Operator.Id.ToString()),
            new Claim("IsAdmin", "true")
        };

        var claimsIdentity = new ClaimsIdentity(sessionClaims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity), authProperties);

        _logger.LogInformation("Admin logged in: {Email}", request.Email);

        return Ok(new
        {
            message = "Login successful",
            provider = new
            {
                id = response.Operator.Id,
                email = response.Operator.Email,
                name = response.Operator.Name,
                isAdmin = response.Operator.IsAdmin
            }
        });
    }

    [HttpPost("requester/register")]
    public async Task<IActionResult> RequesterRegister([FromBody] RequesterRegisterRequest request)
    {
        var command = new RequesterRegisterCommand
        {
            Email = request.Email,
            Password = request.Password,
            Name = request.Name,
            Phone = request.Phone
        };

        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            return BadRequest(new { error = response.Error });
        }

        // Sign in the user
        var sessionClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, response.Requester!.Id.ToString()),
            new Claim(ClaimTypes.Email, response.Requester.Email),
            new Claim(ClaimTypes.Name, response.Requester.Name),
            new Claim("RequesterId", response.Requester.Id.ToString())
        };

        var claimsIdentity = new ClaimsIdentity(sessionClaims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity), authProperties);

        return Ok(new
        {
            message = "Registration successful",
            requester = new
            {
                id = response.Requester.Id,
                email = response.Requester.Email,
                name = response.Requester.Name
            }
        });
    }

    [HttpPost("requester/login")]
    public async Task<IActionResult> RequesterLogin([FromBody] LoginRequest request)
    {
        var command = new RequesterLoginCommand
        {
            Email = request.Email,
            Password = request.Password
        };

        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            return Unauthorized(new { error = response.Error });
        }

        // Sign in the user
        var sessionClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, response.Requester!.Id.ToString()),
            new Claim(ClaimTypes.Email, response.Requester.Email),
            new Claim(ClaimTypes.Name, response.Requester.Name),
            new Claim("RequesterId", response.Requester.Id.ToString())
        };

        var claimsIdentity = new ClaimsIdentity(sessionClaims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity), authProperties);

        _logger.LogInformation("Requester logged in: {Email}", request.Email);

        return Ok(new
        {
            message = "Login successful",
            requester = new
            {
                id = response.Requester.Id,
                email = response.Requester.Email,
                name = response.Requester.Name
            }
        });
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var query = new GetCurrentUserQuery();
        var response = await _mediator.Send(query);

        if (!response.IsAuthenticated)
        {
            return Unauthorized();
        }

        return Ok(new
        {
            id = response.Id,
            email = response.Email,
            name = response.Name,
            userType = response.UserType,
            isAdmin = response.IsAdmin
        });
    }
}

public class RequesterRegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

public class CreateAdminRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
}

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

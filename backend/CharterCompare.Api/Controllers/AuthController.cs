using Microsoft.AspNetCore.Mvc;
using CharterCompare.Api.Services;
using CharterCompare.Api.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using CharterCompare.Api.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

namespace CharterCompare.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IProviderService _providerService;
    private readonly IRequesterService _requesterService;
    private readonly ApplicationDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IProviderService providerService, IRequesterService requesterService, ApplicationDbContext dbContext, IConfiguration configuration, ILogger<AuthController> logger)
    {
        _providerService = providerService;
        _requesterService = requesterService;
        _dbContext = dbContext;
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
        
        // Build redirect URL explicitly to ensure it matches Google Console configuration
        // Use HTTP explicitly for localhost to avoid scheme mismatch
        // Note: Google OAuth only allows one callback URL per client, so we use the same callback
        // and determine user type from the query parameter or properties
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
        
        // Store user type in properties for later use in the OAuth event handler
        var properties = new AuthenticationProperties 
        { 
            RedirectUri = redirectUrl,
            Items = { { "userType", userType ?? "operator" } }
        };
        return Challenge(properties, "Google");
    }

    // Note: The google-callback is handled by the Google authentication middleware
    // The OnCreatingTicket event in Program.cs handles user creation and redirect
    // This endpoint should not be called directly, but we keep it as a fallback
    [HttpGet("google-callback")]
    public IActionResult GoogleCallback()
    {
        // This should not be reached if middleware is working correctly
        // But if it is, check if user is authenticated and redirect
        var frontendUrl = _configuration["Frontend:Url"] ?? "http://localhost:5173";
        
        if (User.Identity?.IsAuthenticated ?? false)
        {
            _logger.LogInformation("User already authenticated, redirecting to dashboard");
            // Check if requester or provider/operator
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
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password) || string.IsNullOrEmpty(request.Name))
        {
            return BadRequest(new { error = "Email, password, and name are required" });
        }

        // Check if provider already exists
        var existingProvider = await _providerService.GetProviderByEmailAsync(request.Email);
        if (existingProvider != null)
        {
            return BadRequest(new { error = "A provider with this email already exists" });
        }

        // Hash password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // Create provider
        var provider = await _providerService.CreateProviderAsync(
            email: request.Email,
            name: request.Name,
            externalId: Guid.NewGuid().ToString(), // Generate unique ID for email providers
            externalProvider: "Email",
            companyName: request.CompanyName,
            passwordHash: passwordHash
        );

        _logger.LogInformation("New provider registered: {Email}", request.Email);

        // Sign in the user
        var sessionClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, provider.Id.ToString()),
            new Claim(ClaimTypes.Email, provider.Email),
            new Claim(ClaimTypes.Name, provider.Name),
            new Claim("ProviderId", provider.Id.ToString())
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
                id = provider.Id,
                email = provider.Email,
                name = provider.Name,
                companyName = provider.CompanyName
            }
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new { error = "Email and password are required" });
        }

        // Find provider by email
        var provider = await _providerService.GetProviderByEmailAsync(request.Email);
        if (provider == null)
        {
            return Unauthorized(new { error = "Invalid email or password" });
        }

        // Check if provider uses email/password authentication
        if (provider.ExternalProvider != "Email")
        {
            return BadRequest(new { error = "This account uses a different login method. Please use the appropriate sign-in option." });
        }

        // Verify password
        var isValidPassword = await _providerService.VerifyPasswordAsync(provider, request.Password);
        if (!isValidPassword)
        {
            return Unauthorized(new { error = "Invalid email or password" });
        }

        // Update last login
        provider.LastLoginAt = DateTime.UtcNow;
        _dbContext.Providers.Update(provider);
        await _dbContext.SaveChangesAsync();

        // Sign in the user
        var sessionClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, provider.Id.ToString()),
            new Claim(ClaimTypes.Email, provider.Email),
            new Claim(ClaimTypes.Name, provider.Name),
            new Claim("ProviderId", provider.Id.ToString())
        };

        var claimsIdentity = new ClaimsIdentity(sessionClaims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity), authProperties);

        _logger.LogInformation("Provider logged in: {Email}", request.Email);

        return Ok(new
        {
            message = "Login successful",
            provider = new
            {
                id = provider.Id,
                email = provider.Email,
                name = provider.Name,
                companyName = provider.CompanyName
            }
        });
    }

    [HttpPost("admin/create-admin")]
    public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminRequest request)
    {
        // Check if any admin already exists
        var existingAdmin = await _dbContext.Providers
            .FirstOrDefaultAsync(p => p.IsAdmin);
        
        if (existingAdmin != null)
        {
            return BadRequest(new { error = "An admin already exists. Only one admin is allowed." });
        }

        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Name))
        {
            return BadRequest(new { error = "Email and name are required" });
        }

        // Check if provider already exists
        var existingProvider = await _providerService.GetProviderByEmailAsync(request.Email);
        
        Provider admin;
        if (existingProvider != null)
        {
            // If user exists, promote them to admin (if no admin exists yet)
            admin = existingProvider;
            
            // Update password if provided
            if (!string.IsNullOrEmpty(request.Password))
            {
                admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            }
            
            // Update name and company if provided
            if (!string.IsNullOrEmpty(request.Name))
            {
                admin.Name = request.Name;
            }
            if (request.CompanyName != null)
            {
                admin.CompanyName = request.CompanyName;
            }
        }
        else
        {
            // Create new admin account
            if (string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { error = "Password is required when creating a new admin account" });
            }
            
            // Hash password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Create admin
            admin = await _providerService.CreateProviderAsync(
                email: request.Email,
                name: request.Name,
                externalId: Guid.NewGuid().ToString(),
                externalProvider: "Email",
                companyName: request.CompanyName,
                passwordHash: passwordHash
            );
        }

        // Set as admin
        admin.IsAdmin = true;
        _dbContext.Providers.Update(admin);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Admin created: {Email}", request.Email);

        return Ok(new
        {
            message = "Admin created successfully",
            email = admin.Email,
            name = admin.Name
        });
    }

    [HttpPost("admin/login")]
    public async Task<IActionResult> AdminLogin([FromBody] LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new { error = "Email and password are required" });
        }

        // Find provider by email
        var provider = await _providerService.GetProviderByEmailAsync(request.Email);
        if (provider == null)
        {
            return Unauthorized(new { error = "Invalid email or password" });
        }

        // Check if user is admin
        if (!provider.IsAdmin)
        {
            return Unauthorized(new { error = "Access denied. Admin privileges required." });
        }

        // Verify password
        var isValidPassword = await _providerService.VerifyPasswordAsync(provider, request.Password);
        if (!isValidPassword)
        {
            return Unauthorized(new { error = "Invalid email or password" });
        }

        // Update last login
        provider.LastLoginAt = DateTime.UtcNow;
        _dbContext.Providers.Update(provider);
        await _dbContext.SaveChangesAsync();

        // Sign in the user
        var sessionClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, provider.Id.ToString()),
            new Claim(ClaimTypes.Email, provider.Email),
            new Claim(ClaimTypes.Name, provider.Name),
            new Claim("ProviderId", provider.Id.ToString()),
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
                id = provider.Id,
                email = provider.Email,
                name = provider.Name,
                isAdmin = provider.IsAdmin
            }
        });
    }

    [HttpPost("requester/register")]
    public async Task<IActionResult> RequesterRegister([FromBody] RequesterRegisterRequest request)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password) || string.IsNullOrEmpty(request.Name))
        {
            return BadRequest(new { error = "Email, password, and name are required" });
        }

        // Check if requester already exists
        var existingRequester = await _requesterService.GetRequesterByEmailAsync(request.Email);
        if (existingRequester != null)
        {
            return BadRequest(new { error = "A requester with this email already exists" });
        }

        // Hash password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // Create requester
        var requester = await _requesterService.CreateRequesterAsync(
            email: request.Email,
            name: request.Name,
            externalId: Guid.NewGuid().ToString(),
            externalProvider: "Email",
            passwordHash: passwordHash,
            phone: request.Phone
        );

        _logger.LogInformation("New requester registered: {Email}", request.Email);

        // Sign in the user
        var sessionClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, requester.Id.ToString()),
            new Claim(ClaimTypes.Email, requester.Email),
            new Claim(ClaimTypes.Name, requester.Name),
            new Claim("RequesterId", requester.Id.ToString())
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
                id = requester.Id,
                email = requester.Email,
                name = requester.Name
            }
        });
    }

    [HttpPost("requester/login")]
    public async Task<IActionResult> RequesterLogin([FromBody] LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new { error = "Email and password are required" });
        }

        // Find requester by email
        var requester = await _requesterService.GetRequesterByEmailAsync(request.Email);
        if (requester == null)
        {
            return Unauthorized(new { error = "Invalid email or password" });
        }

        // Check if requester uses email/password authentication
        if (requester.ExternalProvider != "Email")
        {
            return BadRequest(new { error = "This account uses a different login method. Please use the appropriate sign-in option." });
        }

        // Verify password
        var isValidPassword = await _requesterService.VerifyPasswordAsync(requester, request.Password);
        if (!isValidPassword)
        {
            return Unauthorized(new { error = "Invalid email or password" });
        }

        // Update last login
        requester.LastLoginAt = DateTime.UtcNow;
        _dbContext.Requesters.Update(requester);
        await _dbContext.SaveChangesAsync();

        // Sign in the user
        var sessionClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, requester.Id.ToString()),
            new Claim(ClaimTypes.Email, requester.Email),
            new Claim(ClaimTypes.Name, requester.Name),
            new Claim("RequesterId", requester.Id.ToString())
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
                id = requester.Id,
                email = requester.Email,
                name = requester.Name
            }
        });
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return Unauthorized();
        }

        // Check if user is a provider/operator
        var providerIdClaim = User.FindFirst("ProviderId")?.Value;
        if (!string.IsNullOrEmpty(providerIdClaim) && int.TryParse(providerIdClaim, out var providerId))
        {
            var provider = await _providerService.GetProviderByExternalIdAsync(providerId.ToString(), "Internal");
            if (provider != null)
            {
                var isAdmin = User.FindFirst("IsAdmin")?.Value == "true";
                return Ok(new
                {
                    id = provider.Id,
                    email = provider.Email,
                    name = provider.Name,
                    companyName = provider.CompanyName,
                    isAdmin = isAdmin,
                    userType = "operator"
                });
            }
        }

        // Check if user is a requester
        var requesterIdClaim = User.FindFirst("RequesterId")?.Value;
        if (!string.IsNullOrEmpty(requesterIdClaim) && int.TryParse(requesterIdClaim, out var requesterId))
        {
            var requester = await _requesterService.GetRequesterByExternalIdAsync(requesterId.ToString(), "Internal");
            if (requester != null)
            {
                return Ok(new
                {
                    id = requester.Id,
                    email = requester.Email,
                    name = requester.Name,
                    phone = requester.Phone,
                    userType = "requester"
                });
            }
        }

        return Unauthorized();
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

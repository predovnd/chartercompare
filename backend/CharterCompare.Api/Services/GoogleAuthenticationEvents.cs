using CharterCompare.Api.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace CharterCompare.Api.Services;

public class GoogleAuthenticationEvents
{
    private readonly IProviderService _providerService;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GoogleAuthenticationEvents> _logger;
    private readonly string _frontendUrl;

    public GoogleAuthenticationEvents(
        IProviderService providerService,
        ApplicationDbContext dbContext,
        IConfiguration configuration,
        ILogger<GoogleAuthenticationEvents> logger)
    {
        _providerService = providerService;
        _dbContext = dbContext;
        _logger = logger;
        _frontendUrl = configuration["Frontend:Url"] ?? "http://localhost:5173";
    }

    public async Task OnCreatingTicket(OAuthCreatingTicketContext context)
    {
        // This runs after Google authentication succeeds
        var claims = context.Principal?.Claims.ToList();
        var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
        var externalId = claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        _logger.LogInformation("Google authentication succeeded. Email: {Email}, ExternalId: {ExternalId}", email, externalId);

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(externalId))
        {
            _logger.LogError("Missing required user information from Google");
            context.Fail("Missing required user information");
            return;
        }

        // Get or create provider
        var provider = await _providerService.GetProviderByExternalIdAsync(externalId, "Google");
        if (provider == null)
        {
            provider = await _providerService.CreateProviderAsync(email, name ?? email, externalId, "Google");
            _logger.LogInformation("Created new provider: {ProviderId}", provider.Id);
        }
        else
        {
            provider.LastLoginAt = DateTime.UtcNow;
            _dbContext.Providers.Update(provider);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Updated existing provider: {ProviderId}", provider.Id);
        }

        // Update provider info if name changed
        if (!string.IsNullOrEmpty(name) && provider.Name != name)
        {
            provider.Name = name;
            _dbContext.Providers.Update(provider);
            await _dbContext.SaveChangesAsync();
        }

        // Replace the Google claims with our application claims
        var sessionClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, provider.Id.ToString()),
            new Claim(ClaimTypes.Email, provider.Email),
            new Claim(ClaimTypes.Name, provider.Name),
            new Claim("ProviderId", provider.Id.ToString())
        };

        var claimsIdentity = new ClaimsIdentity(sessionClaims, CookieAuthenticationDefaults.AuthenticationScheme);
        context.Principal = new ClaimsPrincipal(claimsIdentity);
        
        // Set redirect URI
        context.Properties.RedirectUri = $"{_frontendUrl}/provider/dashboard";
        
        _logger.LogInformation("Provider authentication complete. Redirecting to dashboard.");
    }
}

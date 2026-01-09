using CharterCompare.Application.MediatR;
using CharterCompare.Application.Requests.Auth;
using CharterCompare.Application.Storage;
using BCrypt.Net;
using Microsoft.Extensions.Logging;

namespace CharterCompare.Application.Handlers.Auth;

public class RequesterLoginHandler : IRequestHandler<RequesterLoginCommand, RequesterLoginResponse>
{
    private readonly IStorage _storage;
    private readonly ILogger<RequesterLoginHandler> _logger;

    public RequesterLoginHandler(IStorage storage, ILogger<RequesterLoginHandler> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task<RequesterLoginResponse> Handle(RequesterLoginCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return new RequesterLoginResponse
            {
                Success = false,
                Error = "Email and password are required"
            };
        }

        var user = await _storage.GetRequesterByEmailAsync(request.Email, cancellationToken);
        if (user == null)
        {
            return new RequesterLoginResponse
            {
                Success = false,
                Error = "Invalid email or password"
            };
        }

        // Check if requester uses email/password authentication
        if (user.ExternalProvider != "Email")
        {
            return new RequesterLoginResponse
            {
                Success = false,
                Error = "This account uses a different login method. Please use the appropriate sign-in option."
            };
        }

        // Verify password
        if (string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return new RequesterLoginResponse
            {
                Success = false,
                Error = "Invalid email or password"
            };
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _storage.UpdateUserAsync(user, cancellationToken);
        await _storage.SaveChangesAsync(cancellationToken);

        // Link any anonymous requests with matching email to this user account
        try
        {
            var unlinkedRequests = await _storage.GetUnlinkedRequestsByEmailAsync(user.Email, cancellationToken);
            if (unlinkedRequests.Any())
            {
                _logger.LogInformation("Found {Count} unlinked requests for email {Email}, linking to requester {RequesterId}", 
                    unlinkedRequests.Count, user.Email, user.Id);
                
                foreach (var unlinkedRequest in unlinkedRequests)
                {
                    unlinkedRequest.RequesterId = user.Id;
                    await _storage.UpdateCharterRequestAsync(unlinkedRequest, cancellationToken);
                }
                
                await _storage.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Successfully linked {Count} requests to requester {RequesterId}", 
                    unlinkedRequests.Count, user.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error linking anonymous requests to requester {RequesterId}", user.Id);
            // Don't fail login if linking fails
        }

        _logger.LogInformation("Requester logged in: {Email}", request.Email);

        return new RequesterLoginResponse
        {
            Success = true,
            Requester = new RequesterInfo
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                Phone = user.Phone
            }
        };
    }
}

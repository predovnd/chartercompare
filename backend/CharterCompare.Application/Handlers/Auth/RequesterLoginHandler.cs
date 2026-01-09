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

        var requester = await _storage.GetRequesterByEmailAsync(request.Email, cancellationToken);
        if (requester == null)
        {
            return new RequesterLoginResponse
            {
                Success = false,
                Error = "Invalid email or password"
            };
        }

        // Check if requester uses email/password authentication
        if (requester.ExternalProvider != "Email")
        {
            return new RequesterLoginResponse
            {
                Success = false,
                Error = "This account uses a different login method. Please use the appropriate sign-in option."
            };
        }

        // Verify password
        if (string.IsNullOrEmpty(requester.PasswordHash) || !BCrypt.Net.BCrypt.Verify(request.Password, requester.PasswordHash))
        {
            return new RequesterLoginResponse
            {
                Success = false,
                Error = "Invalid email or password"
            };
        }

        // Update last login
        requester.LastLoginAt = DateTime.UtcNow;
        await _storage.UpdateRequesterAsync(requester, cancellationToken);
        await _storage.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Requester logged in: {Email}", request.Email);

        return new RequesterLoginResponse
        {
            Success = true,
            Requester = new RequesterInfo
            {
                Id = requester.Id,
                Email = requester.Email,
                Name = requester.Name,
                Phone = requester.Phone
            }
        };
    }
}

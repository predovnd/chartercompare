using CharterCompare.Application.MediatR;
using CharterCompare.Application.Requests.Auth;
using CharterCompare.Application.Storage;
using CharterCompare.Domain.Enums;
using BCrypt.Net;
using Microsoft.Extensions.Logging;

namespace CharterCompare.Application.Handlers.Auth;

public class OperatorLoginHandler : IRequestHandler<OperatorLoginCommand, OperatorLoginResponse>
{
    private readonly IStorage _storage;
    private readonly ILogger<OperatorLoginHandler> _logger;

    public OperatorLoginHandler(IStorage storage, ILogger<OperatorLoginHandler> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task<OperatorLoginResponse> Handle(OperatorLoginCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return new OperatorLoginResponse
            {
                Success = false,
                Error = "Email and password are required"
            };
        }

        var user = await _storage.GetOperatorByEmailAsync(request.Email, cancellationToken);
        if (user == null || (user.Role != UserRole.Operator && user.Role != UserRole.Admin))
        {
            return new OperatorLoginResponse
            {
                Success = false,
                Error = "Invalid email or password"
            };
        }

        // Check if operator uses email/password authentication
        if (user.ExternalProvider != "Email")
        {
            return new OperatorLoginResponse
            {
                Success = false,
                Error = "This account uses a different login method. Please use the appropriate sign-in option."
            };
        }

        // Verify password
        if (string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return new OperatorLoginResponse
            {
                Success = false,
                Error = "Invalid email or password"
            };
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _storage.UpdateUserAsync(user, cancellationToken);
        await _storage.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Operator logged in: {Email}", request.Email);

        return new OperatorLoginResponse
        {
            Success = true,
            Operator = new OperatorInfo
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                CompanyName = user.CompanyName,
                IsAdmin = user.IsAdmin
            }
        };
    }
}

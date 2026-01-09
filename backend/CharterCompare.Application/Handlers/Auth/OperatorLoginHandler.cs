using CharterCompare.Application.MediatR;
using CharterCompare.Application.Requests.Auth;
using CharterCompare.Application.Storage;
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

        var operatorEntity = await _storage.GetOperatorByEmailAsync(request.Email, cancellationToken);
        if (operatorEntity == null)
        {
            return new OperatorLoginResponse
            {
                Success = false,
                Error = "Invalid email or password"
            };
        }

        // Check if operator uses email/password authentication
        if (operatorEntity.ExternalProvider != "Email")
        {
            return new OperatorLoginResponse
            {
                Success = false,
                Error = "This account uses a different login method. Please use the appropriate sign-in option."
            };
        }

        // Verify password
        if (string.IsNullOrEmpty(operatorEntity.PasswordHash) || !BCrypt.Net.BCrypt.Verify(request.Password, operatorEntity.PasswordHash))
        {
            return new OperatorLoginResponse
            {
                Success = false,
                Error = "Invalid email or password"
            };
        }

        // Update last login
        operatorEntity.LastLoginAt = DateTime.UtcNow;
        await _storage.UpdateOperatorAsync(operatorEntity, cancellationToken);
        await _storage.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Operator logged in: {Email}", request.Email);

        return new OperatorLoginResponse
        {
            Success = true,
            Operator = new OperatorInfo
            {
                Id = operatorEntity.Id,
                Email = operatorEntity.Email,
                Name = operatorEntity.Name,
                CompanyName = operatorEntity.CompanyName,
                IsAdmin = operatorEntity.IsAdmin
            }
        };
    }
}

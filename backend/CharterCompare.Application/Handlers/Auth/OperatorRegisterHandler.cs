using CharterCompare.Application.MediatR;
using CharterCompare.Application.Requests.Auth;
using CharterCompare.Application.Storage;
using CharterCompare.Domain.Entities;
using CharterCompare.Domain.Enums;
using BCrypt.Net;
using Microsoft.Extensions.Logging;

namespace CharterCompare.Application.Handlers.Auth;

public class OperatorRegisterHandler : IRequestHandler<OperatorRegisterCommand, OperatorRegisterResponse>
{
    private readonly IStorage _storage;
    private readonly ILogger<OperatorRegisterHandler> _logger;

    public OperatorRegisterHandler(IStorage storage, ILogger<OperatorRegisterHandler> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task<OperatorRegisterResponse> Handle(OperatorRegisterCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password) || string.IsNullOrEmpty(request.Name))
        {
            return new OperatorRegisterResponse
            {
                Success = false,
                Error = "Email, password, and name are required"
            };
        }

        // Check if user already exists
        var existingUser = await _storage.GetUserByEmailAsync(request.Email, cancellationToken);
        if (existingUser != null)
        {
            return new OperatorRegisterResponse
            {
                Success = false,
                Error = "An operator with this email already exists"
            };
        }

        // Hash password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // Create operator user
        var user = new User
        {
            Email = request.Email,
            Name = request.Name,
            CompanyName = request.CompanyName,
            Phone = request.Phone,
            ExternalId = Guid.NewGuid().ToString(),
            ExternalProvider = "Email",
            PasswordHash = passwordHash,
            Role = UserRole.Operator,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _storage.CreateUserAsync(user, cancellationToken);
        
        // Set default attribute: Bus for operators
        await _storage.AddUserAttributeAsync(user.Id, UserAttributeType.Bus, cancellationToken);
        
        await _storage.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("New operator registered: {Email}", request.Email);

        return new OperatorRegisterResponse
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

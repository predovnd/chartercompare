using CharterCompare.Application.MediatR;
using CharterCompare.Application.Requests.Auth;
using CharterCompare.Application.Storage;
using CharterCompare.Domain.Entities;
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

        // Check if operator already exists
        var existingOperator = await _storage.GetOperatorByEmailAsync(request.Email, cancellationToken);
        if (existingOperator != null)
        {
            return new OperatorRegisterResponse
            {
                Success = false,
                Error = "An operator with this email already exists"
            };
        }

        // Hash password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // Create operator
        var operatorEntity = new Operator
        {
            Email = request.Email,
            Name = request.Name,
            CompanyName = request.CompanyName,
            Phone = request.Phone,
            ExternalId = Guid.NewGuid().ToString(),
            ExternalProvider = "Email",
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _storage.CreateOperatorAsync(operatorEntity, cancellationToken);
        await _storage.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("New operator registered: {Email}", request.Email);

        return new OperatorRegisterResponse
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

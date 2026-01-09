using CharterCompare.Application.MediatR;
using CharterCompare.Application.Requests.Auth;
using CharterCompare.Application.Storage;
using CharterCompare.Domain.Entities;
using BCrypt.Net;
using Microsoft.Extensions.Logging;

namespace CharterCompare.Application.Handlers.Auth;

public class CreateAdminHandler : IRequestHandler<CreateAdminCommand, CreateAdminResponse>
{
    private readonly IStorage _storage;
    private readonly ILogger<CreateAdminHandler> _logger;

    public CreateAdminHandler(IStorage storage, ILogger<CreateAdminHandler> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task<CreateAdminResponse> Handle(CreateAdminCommand request, CancellationToken cancellationToken)
    {
        // Check if any admin already exists
        var allOperators = await _storage.GetAllOperatorsAsync(cancellationToken);
        var existingAdmin = allOperators.FirstOrDefault(o => o.IsAdmin);
        
        if (existingAdmin != null)
        {
            return new CreateAdminResponse
            {
                Success = false,
                Error = "An admin already exists. Only one admin is allowed."
            };
        }

        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Name))
        {
            return new CreateAdminResponse
            {
                Success = false,
                Error = "Email and name are required"
            };
        }

        // Check if operator already exists
        var existingOperator = await _storage.GetOperatorByEmailAsync(request.Email, cancellationToken);
        
        Operator admin;
        if (existingOperator != null)
        {
            // If user exists, promote them to admin
            admin = existingOperator;
            
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
                return new CreateAdminResponse
                {
                    Success = false,
                    Error = "Password is required when creating a new admin account"
                };
            }
            
            // Hash password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Create admin
            admin = new Operator
            {
                Email = request.Email,
                Name = request.Name,
                CompanyName = request.CompanyName,
                ExternalId = Guid.NewGuid().ToString(),
                ExternalProvider = "Email",
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _storage.CreateOperatorAsync(admin, cancellationToken);
        }

        // Set as admin
        admin.IsAdmin = true;
        await _storage.UpdateOperatorAsync(admin, cancellationToken);
        await _storage.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Admin created: {Email}", request.Email);

        return new CreateAdminResponse
        {
            Success = true,
            Email = admin.Email,
            Name = admin.Name
        };
    }
}

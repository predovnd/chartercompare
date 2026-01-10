using CharterCompare.Application.MediatR;
using CharterCompare.Application.Requests.Auth;
using CharterCompare.Application.Storage;
using CharterCompare.Domain.Entities;
using CharterCompare.Domain.Enums;
using BCrypt.Net;
using Microsoft.Extensions.Logging;

namespace CharterCompare.Application.Handlers.Auth;

public class RequesterRegisterHandler : IRequestHandler<RequesterRegisterCommand, RequesterRegisterResponse>
{
    private readonly IStorage _storage;
    private readonly ILogger<RequesterRegisterHandler> _logger;

    public RequesterRegisterHandler(IStorage storage, ILogger<RequesterRegisterHandler> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task<RequesterRegisterResponse> Handle(RequesterRegisterCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password) || string.IsNullOrEmpty(request.Name))
        {
            return new RequesterRegisterResponse
            {
                Success = false,
                Error = "Email, password, and name are required"
            };
        }

        // Check if user already exists
        var existingUser = await _storage.GetUserByEmailAsync(request.Email, cancellationToken);
        if (existingUser != null)
        {
            return new RequesterRegisterResponse
            {
                Success = false,
                Error = "A requester with this email already exists"
            };
        }

        // Hash password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // Create requester user - always Individual by default
        // Only admins can change requester type to Business
        var user = new User
        {
            Email = request.Email,
            Name = request.Name,
            Phone = request.Phone,
            CompanyName = null, // CompanyName only set by admin when changing to Business
            ExternalId = Guid.NewGuid().ToString(),
            ExternalProvider = "Email",
            PasswordHash = passwordHash,
            Role = UserRole.Requester,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _storage.CreateUserAsync(user, cancellationToken);
        
        // Set default attribute: Individual for requesters
        await _storage.AddUserAttributeAsync(user.Id, UserAttributeType.Individual, cancellationToken);
        
        await _storage.SaveChangesAsync(cancellationToken);

        // Link any anonymous requests with matching email to this new user account
        try
        {
            var unlinkedRequests = await _storage.GetUnlinkedRequestsByEmailAsync(user.Email, cancellationToken);
            if (unlinkedRequests.Any())
            {
                _logger.LogInformation("Found {Count} unlinked requests for email {Email}, linking to new requester {RequesterId}", 
                    unlinkedRequests.Count, user.Email, user.Id);
                
                foreach (var unlinkedRequest in unlinkedRequests)
                {
                    unlinkedRequest.RequesterId = user.Id;
                    await _storage.UpdateCharterRequestAsync(unlinkedRequest, cancellationToken);
                }
                
                await _storage.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Successfully linked {Count} requests to new requester {RequesterId}", 
                    unlinkedRequests.Count, user.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error linking anonymous requests to new requester {RequesterId}", user.Id);
            // Don't fail registration if linking fails
        }

        _logger.LogInformation("New requester registered: {Email}", request.Email);

        return new RequesterRegisterResponse
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

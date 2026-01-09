using CharterCompare.Application.MediatR;
using CharterCompare.Application.Requests.Auth;
using CharterCompare.Application.Storage;
using CharterCompare.Domain.Entities;
using BCrypt.Net;
using Microsoft.Extensions.Logging;
using RequesterEntity = CharterCompare.Domain.Entities.Requester;

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

        // Check if requester already exists
        var existingRequester = await _storage.GetRequesterByEmailAsync(request.Email, cancellationToken);
        if (existingRequester != null)
        {
            return new RequesterRegisterResponse
            {
                Success = false,
                Error = "A requester with this email already exists"
            };
        }

        // Hash password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // Create requester
        var requester = new RequesterEntity
        {
            Email = request.Email,
            Name = request.Name,
            Phone = request.Phone,
            ExternalId = Guid.NewGuid().ToString(),
            ExternalProvider = "Email",
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _storage.CreateRequesterAsync(requester, cancellationToken);
        await _storage.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("New requester registered: {Email}", request.Email);

        return new RequesterRegisterResponse
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

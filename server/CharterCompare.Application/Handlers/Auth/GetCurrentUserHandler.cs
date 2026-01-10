using CharterCompare.Application.MediatR;
using CharterCompare.Application.Requests.Auth;
using CharterCompare.Application.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CharterCompare.Application.Handlers.Auth;

public class GetCurrentUserHandler : IRequestHandler<GetCurrentUserQuery, GetCurrentUserResponse>
{
    private readonly IStorage _storage;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<GetCurrentUserHandler> _logger;

    public GetCurrentUserHandler(IStorage storage, IHttpContextAccessor httpContextAccessor, ILogger<GetCurrentUserHandler> logger)
    {
        _storage = storage;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<GetCurrentUserResponse> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            return new GetCurrentUserResponse
            {
                IsAuthenticated = false
            };
        }

        // Check for UserId claim (unified user model)
        var userIdClaim = httpContext.User.FindFirst("UserId")?.Value;
        if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
        {
            var user = await _storage.GetUserByIdAsync(userId, cancellationToken);
            if (user != null)
            {
                return new GetCurrentUserResponse
                {
                    IsAuthenticated = true,
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    UserType = user.Role == Domain.Enums.UserRole.Admin ? "admin" : 
                               user.Role == Domain.Enums.UserRole.Operator ? "operator" : "requester",
                    IsAdmin = user.IsAdmin
                };
            }
        }

        // Fallback: Check legacy claims for backward compatibility during migration
        var providerIdClaim = httpContext.User.FindFirst("ProviderId")?.Value;
        if (!string.IsNullOrEmpty(providerIdClaim) && int.TryParse(providerIdClaim, out var providerId))
        {
            var user = await _storage.GetOperatorByIdAsync(providerId, cancellationToken);
            if (user != null)
            {
                return new GetCurrentUserResponse
                {
                    IsAuthenticated = true,
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    UserType = user.IsAdmin ? "admin" : "operator",
                    IsAdmin = user.IsAdmin
                };
            }
        }

        var requesterIdClaim = httpContext.User.FindFirst("RequesterId")?.Value;
        if (!string.IsNullOrEmpty(requesterIdClaim) && int.TryParse(requesterIdClaim, out var requesterId))
        {
            var user = await _storage.GetRequesterByIdAsync(requesterId, cancellationToken);
            if (user != null)
            {
                return new GetCurrentUserResponse
                {
                    IsAuthenticated = true,
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    UserType = "requester",
                    IsAdmin = false
                };
            }
        }

        return new GetCurrentUserResponse
        {
            IsAuthenticated = false
        };
    }
}

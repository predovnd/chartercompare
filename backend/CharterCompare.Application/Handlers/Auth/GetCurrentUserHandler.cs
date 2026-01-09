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

        // Check if user is an operator
        var providerIdClaim = httpContext.User.FindFirst("ProviderId")?.Value;
        if (!string.IsNullOrEmpty(providerIdClaim) && int.TryParse(providerIdClaim, out var providerId))
        {
            var operatorEntity = await _storage.GetOperatorByIdAsync(providerId, cancellationToken);
            if (operatorEntity != null)
            {
                return new GetCurrentUserResponse
                {
                    IsAuthenticated = true,
                    Id = operatorEntity.Id,
                    Email = operatorEntity.Email,
                    Name = operatorEntity.Name,
                    UserType = "operator",
                    IsAdmin = operatorEntity.IsAdmin
                };
            }
        }

        // Check if user is a requester
        var requesterIdClaim = httpContext.User.FindFirst("RequesterId")?.Value;
        if (!string.IsNullOrEmpty(requesterIdClaim) && int.TryParse(requesterIdClaim, out var requesterId))
        {
            var requester = await _storage.GetRequesterByIdAsync(requesterId, cancellationToken);
            if (requester != null)
            {
                return new GetCurrentUserResponse
                {
                    IsAuthenticated = true,
                    Id = requester.Id,
                    Email = requester.Email,
                    Name = requester.Name,
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

using CharterCompare.Application.MediatR;
using CharterCompare.Application.Requests.Admin;
using CharterCompare.Application.Storage;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace CharterCompare.Application.Handlers.Admin;

public class GetAdminUsersHandler : IRequestHandler<GetAdminUsersQuery, GetAdminUsersResponse>
{
    private readonly IStorage _storage;
    private readonly ILogger<GetAdminUsersHandler> _logger;

    public GetAdminUsersHandler(IStorage storage, ILogger<GetAdminUsersHandler> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task<GetAdminUsersResponse> Handle(GetAdminUsersQuery request, CancellationToken cancellationToken)
    {
        var allUsers = await _storage.GetAllUsersAsync(cancellationToken);
        var allRequests = await _storage.GetAllCharterRequestsAsync(cancellationToken);
        
        var userDtos = allUsers.Select(u => new UserDto
        {
            Id = u.Id,
            Email = u.Email,
            Name = u.Name,
            CompanyName = u.CompanyName,
            Phone = u.Phone,
            ExternalProvider = u.ExternalProvider,
            IsAdmin = u.IsAdmin,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt,
            LastLoginAt = u.LastLoginAt,
            QuoteCount = u.Quotes.Count,
            RequestCount = u.Role == Domain.Enums.UserRole.Requester 
                ? u.Requests.Count 
                : allRequests.Count(r => r.Quotes.Any(q => q.ProviderId == u.Id)),
            UserType = u.Role == Domain.Enums.UserRole.Admin ? "admin" :
                       u.Role == Domain.Enums.UserRole.Operator ? "operator" : "requester",
            Attributes = u.Attributes.Select(a => a.AttributeType.ToString()).ToList()
        }).OrderBy(u => u.CreatedAt).ToList();

        return new GetAdminUsersResponse
        {
            Users = userDtos
        };
    }
}

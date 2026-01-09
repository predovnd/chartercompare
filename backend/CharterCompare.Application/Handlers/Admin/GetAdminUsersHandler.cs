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
        var operators = await _storage.GetAllOperatorsAsync(cancellationToken);
        var requesters = await _storage.GetAllRequestersAsync(cancellationToken);
        var allRequests = await _storage.GetAllCharterRequestsAsync(cancellationToken);
        
        var operatorUsers = operators.Select(o => new UserDto
        {
            Id = o.Id,
            Email = o.Email,
            Name = o.Name,
            CompanyName = o.CompanyName,
            Phone = o.Phone,
            ExternalProvider = o.ExternalProvider,
            IsAdmin = o.IsAdmin,
            IsActive = o.IsActive,
            CreatedAt = o.CreatedAt,
            LastLoginAt = o.LastLoginAt,
            QuoteCount = o.Quotes.Count,
            RequestCount = allRequests.Count(r => r.Quotes.Any(q => q.ProviderId == o.Id)),
            UserType = "operator"
        }).ToList();

        var requesterUsers = requesters.Select(r => new UserDto
        {
            Id = r.Id,
            Email = r.Email,
            Name = r.Name,
            CompanyName = null,
            Phone = r.Phone,
            ExternalProvider = r.ExternalProvider,
            IsAdmin = false,
            IsActive = r.IsActive,
            CreatedAt = r.CreatedAt,
            LastLoginAt = r.LastLoginAt,
            QuoteCount = 0,
            RequestCount = r.Requests.Count,
            UserType = "requester"
        }).ToList();

        var allUsers = operatorUsers.Concat(requesterUsers).OrderBy(u => u.CreatedAt).ToList();

        return new GetAdminUsersResponse
        {
            Users = allUsers
        };
    }
}

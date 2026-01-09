using CharterCompare.Application.MediatR;
using CharterCompare.Application.Requests.Admin;
using CharterCompare.Application.Storage;
using CharterCompare.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CharterCompare.Application.Handlers.Admin;

public class GetAdminStatsHandler : IRequestHandler<GetAdminStatsQuery, GetAdminStatsResponse>
{
    private readonly IStorage _storage;
    private readonly ILogger<GetAdminStatsHandler> _logger;

    public GetAdminStatsHandler(IStorage storage, ILogger<GetAdminStatsHandler> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task<GetAdminStatsResponse> Handle(GetAdminStatsQuery request, CancellationToken cancellationToken)
    {
        var allRequests = await _storage.GetAllCharterRequestsAsync(cancellationToken);
        var allQuotes = allRequests.SelectMany(r => r.Quotes).ToList();
        var allUsers = await _storage.GetAllUsersAsync(cancellationToken);
        
        var operators = allUsers.Where(u => u.Role == Domain.Enums.UserRole.Operator || u.Role == Domain.Enums.UserRole.Admin).Count();
        var requesters = allUsers.Where(u => u.Role == Domain.Enums.UserRole.Requester).Count();

        return new GetAdminStatsResponse
        {
            TotalRequests = allRequests.Count,
            OpenRequests = allRequests.Count(r => r.Status == RequestStatus.Draft || r.Status == RequestStatus.UnderReview),
            TotalQuotes = allQuotes.Count,
            TotalOperators = operators,
            TotalRequesters = requesters
        };
    }
}

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
        var allOperators = await _storage.GetAllOperatorsAsync(cancellationToken);
        
        // Count requesters - we'll need to get unique requester IDs from requests
        var uniqueRequesterIds = allRequests
            .Where(r => r.RequesterId.HasValue)
            .Select(r => r.RequesterId!.Value)
            .Distinct()
            .Count();

        return new GetAdminStatsResponse
        {
            TotalRequests = allRequests.Count,
            OpenRequests = allRequests.Count(r => r.Status == RequestStatus.Open),
            TotalQuotes = allQuotes.Count,
            TotalOperators = allOperators.Count,
            TotalRequesters = uniqueRequesterIds
        };
    }
}

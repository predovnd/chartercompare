using CharterCompare.Application.MediatR;
using CharterCompare.Application.Requests.Admin;
using CharterCompare.Application.Storage;
using Microsoft.Extensions.Logging;

namespace CharterCompare.Application.Handlers.Admin;

public class GetOperatorCoveragesHandler : IRequestHandler<GetOperatorCoveragesQuery, GetOperatorCoveragesResponse>
{
    private readonly IStorage _storage;
    private readonly ILogger<GetOperatorCoveragesHandler> _logger;

    public GetOperatorCoveragesHandler(IStorage storage, ILogger<GetOperatorCoveragesHandler> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task<GetOperatorCoveragesResponse> Handle(GetOperatorCoveragesQuery request, CancellationToken cancellationToken)
    {
        var coverages = await _storage.GetOperatorCoveragesByOperatorIdAsync(request.OperatorId, cancellationToken);
        
        var coverageDtos = coverages.Select(c => new OperatorCoverageDto
        {
            Id = c.Id,
            OperatorId = c.OperatorId,
            BaseLocationName = c.BaseLocationName,
            Latitude = c.Latitude,
            Longitude = c.Longitude,
            CoverageRadiusKm = c.CoverageRadiusKm,
            MinPassengerCapacity = c.MinPassengerCapacity,
            MaxPassengerCapacity = c.MaxPassengerCapacity,
            IsGeocoded = c.IsGeocoded,
            GeocodingError = c.GeocodingError,
            CreatedAt = c.CreatedAt
        }).ToList();

        return new GetOperatorCoveragesResponse
        {
            Coverages = coverageDtos
        };
    }
}

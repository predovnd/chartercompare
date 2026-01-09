using CharterCompare.Application.MediatR;
using CharterCompare.Application.Requests.Admin;
using CharterCompare.Application.Services;
using CharterCompare.Application.Storage;
using Microsoft.Extensions.Logging;

namespace CharterCompare.Application.Handlers.Admin;

public class UpdateOperatorCoverageHandler : IRequestHandler<UpdateOperatorCoverageCommand, UpdateOperatorCoverageResponse>
{
    private readonly IStorage _storage;
    private readonly IGeocodingService _geocodingService;
    private readonly ILogger<UpdateOperatorCoverageHandler> _logger;

    public UpdateOperatorCoverageHandler(
        IStorage storage,
        IGeocodingService geocodingService,
        ILogger<UpdateOperatorCoverageHandler> logger)
    {
        _storage = storage;
        _geocodingService = geocodingService;
        _logger = logger;
    }

    public async Task<UpdateOperatorCoverageResponse> Handle(UpdateOperatorCoverageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get existing coverage
            var coverage = await _storage.GetOperatorCoverageByIdAsync(request.CoverageId, cancellationToken);
            if (coverage == null)
            {
                return new UpdateOperatorCoverageResponse
                {
                    Success = false,
                    Error = "Coverage not found"
                };
            }

            // Validate capacity range
            if (request.MinPassengerCapacity < 1 || request.MaxPassengerCapacity < 1)
            {
                return new UpdateOperatorCoverageResponse
                {
                    Success = false,
                    Error = "Passenger capacity must be at least 1"
                };
            }

            if (request.MinPassengerCapacity > request.MaxPassengerCapacity)
            {
                return new UpdateOperatorCoverageResponse
                {
                    Success = false,
                    Error = "Minimum capacity cannot be greater than maximum capacity"
                };
            }

            if (request.CoverageRadiusKm <= 0)
            {
                return new UpdateOperatorCoverageResponse
                {
                    Success = false,
                    Error = "Coverage radius must be greater than 0"
                };
            }

            // Update location name
            coverage.BaseLocationName = request.BaseLocationName.Trim();
            coverage.CoverageRadiusKm = request.CoverageRadiusKm;
            coverage.MinPassengerCapacity = request.MinPassengerCapacity;
            coverage.MaxPassengerCapacity = request.MaxPassengerCapacity;
            coverage.UpdatedAt = DateTime.UtcNow;

            // Re-geocode if location name changed
            if (coverage.BaseLocationName != request.BaseLocationName || !coverage.IsGeocoded)
            {
                _logger.LogInformation("Geocoding location: {Location} for coverage {CoverageId}", request.BaseLocationName, request.CoverageId);
                var geocodingResult = await _geocodingService.GeocodeAsync(request.BaseLocationName, cancellationToken);

                if (geocodingResult != null)
                {
                    coverage.Latitude = geocodingResult.Latitude;
                    coverage.Longitude = geocodingResult.Longitude;
                    coverage.IsGeocoded = true;
                    coverage.GeocodingError = null;
                    _logger.LogInformation("Successfully geocoded {Location} to {Lat}, {Lon}", request.BaseLocationName, geocodingResult.Latitude, geocodingResult.Longitude);
                }
                else
                {
                    coverage.IsGeocoded = false;
                    coverage.GeocodingError = "Could not resolve location. Please check the location name and try again.";
                    _logger.LogWarning("Failed to geocode location: {Location}", request.BaseLocationName);
                }
            }

            await _storage.UpdateOperatorCoverageAsync(coverage, cancellationToken);

            return new UpdateOperatorCoverageResponse
            {
                Success = true,
                Coverage = new OperatorCoverageDto
                {
                    Id = coverage.Id,
                    OperatorId = coverage.OperatorId,
                    BaseLocationName = coverage.BaseLocationName,
                    Latitude = coverage.Latitude,
                    Longitude = coverage.Longitude,
                    CoverageRadiusKm = coverage.CoverageRadiusKm,
                    MinPassengerCapacity = coverage.MinPassengerCapacity,
                    MaxPassengerCapacity = coverage.MaxPassengerCapacity,
                    IsGeocoded = coverage.IsGeocoded,
                    GeocodingError = coverage.GeocodingError,
                    CreatedAt = coverage.CreatedAt
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating operator coverage: {Error}", ex.Message);
            return new UpdateOperatorCoverageResponse
            {
                Success = false,
                Error = $"An error occurred: {ex.Message}"
            };
        }
    }
}

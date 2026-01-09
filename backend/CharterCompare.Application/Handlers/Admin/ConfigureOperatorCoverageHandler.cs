using CharterCompare.Application.MediatR;
using CharterCompare.Application.Requests.Admin;
using CharterCompare.Application.Services;
using CharterCompare.Application.Storage;
using CharterCompare.Domain.Entities;
using CharterCompare.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CharterCompare.Application.Handlers.Admin;

public class ConfigureOperatorCoverageHandler : IRequestHandler<ConfigureOperatorCoverageCommand, ConfigureOperatorCoverageResponse>
{
    private readonly IStorage _storage;
    private readonly IGeocodingService _geocodingService;
    private readonly ILogger<ConfigureOperatorCoverageHandler> _logger;

    public ConfigureOperatorCoverageHandler(
        IStorage storage,
        IGeocodingService geocodingService,
        ILogger<ConfigureOperatorCoverageHandler> logger)
    {
        _storage = storage;
        _geocodingService = geocodingService;
        _logger = logger;
    }

    public async Task<ConfigureOperatorCoverageResponse> Handle(ConfigureOperatorCoverageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Verify operator exists and is actually an operator
            var operatorUser = await _storage.GetOperatorByIdAsync(request.OperatorId, cancellationToken);
            if (operatorUser == null)
            {
                return new ConfigureOperatorCoverageResponse
                {
                    Success = false,
                    Error = "Operator not found"
                };
            }

            // Check if operator already has coverage (only one allowed for now)
            var existingCoverages = await _storage.GetOperatorCoveragesByOperatorIdAsync(request.OperatorId, cancellationToken);
            if (existingCoverages.Count > 0)
            {
                return new ConfigureOperatorCoverageResponse
                {
                    Success = false,
                    Error = "Operator already has coverage configured. Please edit or delete the existing coverage first."
                };
            }

            // Validate capacity range
            if (request.MinPassengerCapacity < 1 || request.MaxPassengerCapacity < 1)
            {
                return new ConfigureOperatorCoverageResponse
                {
                    Success = false,
                    Error = "Passenger capacity must be at least 1"
                };
            }

            if (request.MinPassengerCapacity > request.MaxPassengerCapacity)
            {
                return new ConfigureOperatorCoverageResponse
                {
                    Success = false,
                    Error = "Minimum capacity cannot be greater than maximum capacity"
                };
            }

            if (request.CoverageRadiusKm <= 0)
            {
                return new ConfigureOperatorCoverageResponse
                {
                    Success = false,
                    Error = "Coverage radius must be greater than 0"
                };
            }

            // Geocode the location
            _logger.LogInformation("Geocoding location: {Location} for operator {OperatorId}", request.BaseLocationName, request.OperatorId);
            var geocodingResult = await _geocodingService.GeocodeAsync(request.BaseLocationName, cancellationToken);

            // Create coverage record
            var coverage = new OperatorCoverage
            {
                OperatorId = request.OperatorId,
                BaseLocationName = request.BaseLocationName.Trim(),
                CoverageRadiusKm = request.CoverageRadiusKm,
                MinPassengerCapacity = request.MinPassengerCapacity,
                MaxPassengerCapacity = request.MaxPassengerCapacity,
                CreatedAt = DateTime.UtcNow
            };

            if (geocodingResult != null)
            {
                coverage.Latitude = geocodingResult.Latitude;
                coverage.Longitude = geocodingResult.Longitude;
                coverage.IsGeocoded = true;
                _logger.LogInformation("Successfully geocoded {Location} to {Lat}, {Lon}", request.BaseLocationName, geocodingResult.Latitude, geocodingResult.Longitude);
            }
            else
            {
                coverage.IsGeocoded = false;
                coverage.GeocodingError = "Could not resolve location. Please check the location name and try again.";
                _logger.LogWarning("Failed to geocode location: {Location}", request.BaseLocationName);
            }

            var savedCoverage = await _storage.CreateOperatorCoverageAsync(coverage, cancellationToken);

            return new ConfigureOperatorCoverageResponse
            {
                Success = true,
                Coverage = new OperatorCoverageDto
                {
                    Id = savedCoverage.Id,
                    OperatorId = savedCoverage.OperatorId,
                    BaseLocationName = savedCoverage.BaseLocationName,
                    Latitude = savedCoverage.Latitude,
                    Longitude = savedCoverage.Longitude,
                    CoverageRadiusKm = savedCoverage.CoverageRadiusKm,
                    MinPassengerCapacity = savedCoverage.MinPassengerCapacity,
                    MaxPassengerCapacity = savedCoverage.MaxPassengerCapacity,
                    IsGeocoded = savedCoverage.IsGeocoded,
                    GeocodingError = savedCoverage.GeocodingError,
                    CreatedAt = savedCoverage.CreatedAt
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring operator coverage: {Error}", ex.Message);
            return new ConfigureOperatorCoverageResponse
            {
                Success = false,
                Error = $"An error occurred: {ex.Message}"
            };
        }
    }
}

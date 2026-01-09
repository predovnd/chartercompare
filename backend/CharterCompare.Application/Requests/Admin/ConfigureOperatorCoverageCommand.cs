using CharterCompare.Application.MediatR;

namespace CharterCompare.Application.Requests.Admin;

public class ConfigureOperatorCoverageCommand : IRequest<ConfigureOperatorCoverageResponse>
{
    public int OperatorId { get; set; }
    public string BaseLocationName { get; set; } = string.Empty;
    public double CoverageRadiusKm { get; set; }
    public int MinPassengerCapacity { get; set; }
    public int MaxPassengerCapacity { get; set; }
}

public class ConfigureOperatorCoverageResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public OperatorCoverageDto? Coverage { get; set; }
}

public class OperatorCoverageDto
{
    public int Id { get; set; }
    public int OperatorId { get; set; }
    public string BaseLocationName { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double CoverageRadiusKm { get; set; }
    public int MinPassengerCapacity { get; set; }
    public int MaxPassengerCapacity { get; set; }
    public bool IsGeocoded { get; set; }
    public string? GeocodingError { get; set; }
    public DateTime CreatedAt { get; set; }
}

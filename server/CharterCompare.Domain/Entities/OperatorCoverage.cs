namespace CharterCompare.Domain.Entities;

public class OperatorCoverage
{
    public int Id { get; set; }
    public int OperatorId { get; set; }
    public User Operator { get; set; } = null!;
    
    // Human-friendly location (what admin enters)
    public string BaseLocationName { get; set; } = string.Empty; // e.g., "Sydney, NSW" or "123 Main St, Melbourne"
    
    // Auto-resolved coordinates (admin never sees/edits these)
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    
    // Coverage radius in kilometers
    public double CoverageRadiusKm { get; set; }
    
    // Passenger capacity range
    public int MinPassengerCapacity { get; set; }
    public int MaxPassengerCapacity { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Indicates if geocoding was successful
    public bool IsGeocoded { get; set; }
    public string? GeocodingError { get; set; }
}

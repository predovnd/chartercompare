namespace CharterCompare.Application.Services;

public interface IGeocodingService
{
    /// <summary>
    /// Resolves a place name (e.g., "Sydney, NSW" or "123 Main St, Melbourne") to coordinates
    /// </summary>
    /// <param name="placeName">Human-friendly place name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Geocoding result with lat/lng, or null if not found</returns>
    Task<GeocodingResult?> GeocodeAsync(string placeName, CancellationToken cancellationToken = default);
}

public class GeocodingResult
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? DisplayName { get; set; } // Full formatted address from geocoding service
}

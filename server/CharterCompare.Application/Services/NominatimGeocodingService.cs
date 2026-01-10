using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CharterCompare.Application.Services;

/// <summary>
/// Geocoding service using OpenStreetMap Nominatim (free, no API key required)
/// </summary>
public class NominatimGeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NominatimGeocodingService> _logger;
    private const string NominatimBaseUrl = "https://nominatim.openstreetmap.org";

    public NominatimGeocodingService(HttpClient httpClient, ILogger<NominatimGeocodingService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Nominatim requires a user agent
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "CharterCompare/1.0");
    }

    public async Task<GeocodingResult?> GeocodeAsync(string placeName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(placeName))
        {
            return null;
        }

        try
        {
            // URL encode the place name
            var encodedPlace = Uri.EscapeDataString(placeName);
            var url = $"{NominatimBaseUrl}/search?q={encodedPlace}&format=json&limit=1";

            _logger.LogInformation("Geocoding place: {PlaceName}", placeName);

            var response = await _httpClient.GetStringAsync(url, cancellationToken);
            
            var results = JsonSerializer.Deserialize<List<NominatimResponse>>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (results == null || results.Count == 0)
            {
                _logger.LogWarning("No geocoding results found for: {PlaceName}", placeName);
                return null;
            }

            var firstResult = results[0];
            
            if (double.TryParse(firstResult.Lat, out var lat) && double.TryParse(firstResult.Lon, out var lon))
            {
                _logger.LogInformation("Geocoded {PlaceName} to {Lat}, {Lon}", placeName, lat, lon);
                return new GeocodingResult
                {
                    Latitude = lat,
                    Longitude = lon,
                    DisplayName = firstResult.DisplayName
                };
            }

            _logger.LogWarning("Failed to parse coordinates for: {PlaceName}", placeName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error geocoding place: {PlaceName}", placeName);
            return null;
        }
    }

    private class NominatimResponse
    {
        public string Lat { get; set; } = string.Empty;
        public string Lon { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }
}

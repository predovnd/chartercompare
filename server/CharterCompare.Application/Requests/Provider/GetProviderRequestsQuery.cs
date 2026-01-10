using CharterCompare.Application.MediatR;

namespace CharterCompare.Application.Requests.Provider;

public class GetProviderRequestsQuery : IRequest<GetProviderRequestsResponse>
{
}

public class GetProviderRequestsResponse
{
    public List<ProviderRequestDto> Requests { get; set; } = new();
}

public class ProviderRequestDto
{
    public int Id { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public CharterRequestDto RequestData { get; set; } = null!;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int QuoteCount { get; set; }
    public bool HasSubmittedQuote { get; set; }
}

public class CharterRequestDto
{
    public CustomerInfoDto Customer { get; set; } = new();
    public TripInfoDto Trip { get; set; } = new();
    public RequestMetaDto Meta { get; set; } = new();
}

public class CustomerInfoDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class TripInfoDto
{
    public string Type { get; set; } = string.Empty;
    public int PassengerCount { get; set; }
    public DateInfoDto Date { get; set; } = new();
    public LocationInfoDto PickupLocation { get; set; } = new();
    public LocationInfoDto Destination { get; set; } = new();
    public string TripFormat { get; set; } = string.Empty;
    public TimingInfoDto Timing { get; set; } = new();
    public List<string> SpecialRequirements { get; set; } = new();
}

public class DateInfoDto
{
    public string RawInput { get; set; } = string.Empty;
    public string ResolvedDate { get; set; } = string.Empty;
    public string Confidence { get; set; } = string.Empty;
}

public class LocationInfoDto
{
    public string RawInput { get; set; } = string.Empty;
    public string ResolvedName { get; set; } = string.Empty;
    public string Suburb { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public string Confidence { get; set; } = string.Empty;
}

public class TimingInfoDto
{
    public string RawInput { get; set; } = string.Empty;
    public string PickupTime { get; set; } = string.Empty;
    public string ReturnTime { get; set; } = string.Empty;
}

public class RequestMetaDto
{
    public string Source { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}

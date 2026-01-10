using CharterCompare.Application.MediatR;

namespace CharterCompare.Application.Requests.Admin;

public class GetAdminRequestsQuery : IRequest<GetAdminRequestsResponse>
{
}

public class GetAdminRequestsResponse
{
    public List<AdminRequestDto> Requests { get; set; } = new();
}

public class AdminRequestDto
{
    public int Id { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public CharterRequestDto RequestData { get; set; } = null!;
    public string? RawJsonPayload { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int? RequesterId { get; set; }
    public string? RequesterEmail { get; set; }
    public string? RequesterName { get; set; }
    public int QuoteCount { get; set; }
    public bool HasLowConfidence { get; set; }
    public List<AdminQuoteDto> Quotes { get; set; } = new();
}

public class AdminQuoteDto
{
    public int Id { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string ProviderEmail { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// Reuse DTOs from Provider namespace
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

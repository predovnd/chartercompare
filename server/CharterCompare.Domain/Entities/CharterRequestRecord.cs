using CharterCompare.Domain.Enums;

namespace CharterCompare.Domain.Entities;

public class CharterRequestRecord
{
    public int Id { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public CharterRequest RequestData { get; set; } = null!;
    public string? RawJsonPayload { get; set; } // Store the raw JSON payload for admin viewing
    public int? RequesterId { get; set; } // Link to requester account (nullable for anonymous requests)
    public User? Requester { get; set; }
    public string? Email { get; set; } // Email from request (for anonymous users or quick access)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public RequestStatus Status { get; set; } = RequestStatus.Draft;
    public DateTime? QuoteDeadline { get; set; } // 24 hours from when request is published
    public List<Quote> Quotes { get; set; } = new();
}

public class CharterRequest
{
    public CustomerInfo Customer { get; set; } = new();
    public TripInfo Trip { get; set; } = new();
    public RequestMeta Meta { get; set; } = new();
}

public class CustomerInfo
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class TripInfo
{
    public string Type { get; set; } = string.Empty;
    public int PassengerCount { get; set; }
    public DateInfo Date { get; set; } = new();
    public LocationInfo PickupLocation { get; set; } = new();
    public LocationInfo Destination { get; set; } = new();
    public string TripFormat { get; set; } = string.Empty; // "one_way" or "return_same_day"
    public TimingInfo Timing { get; set; } = new();
    public List<string> SpecialRequirements { get; set; } = new();
}

public class DateInfo
{
    public string RawInput { get; set; } = string.Empty;
    public string ResolvedDate { get; set; } = string.Empty;
    public string Confidence { get; set; } = string.Empty; // "low", "medium", "high"
}

public class LocationInfo
{
    public string RawInput { get; set; } = string.Empty;
    public string ResolvedName { get; set; } = string.Empty;
    public string Suburb { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public string Confidence { get; set; } = string.Empty; // "low" or "medium"
}

public class TimingInfo
{
    public string RawInput { get; set; } = string.Empty;
    public string PickupTime { get; set; } = string.Empty;
    public string ReturnTime { get; set; } = string.Empty;
}

public class RequestMeta
{
    public string Source { get; set; } = "webchat";
    public string CreatedAt { get; set; } = string.Empty;
}

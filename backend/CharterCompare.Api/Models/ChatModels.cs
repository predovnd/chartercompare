namespace CharterCompare.Api.Models;

public class ChatMessage
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Sender { get; set; } = string.Empty; // "user" or "bot"
    public DateTime Timestamp { get; set; }
    public string? Icon { get; set; }
}

public class StartChatRequest
{
    // Can add user context here if needed
}

public class StartChatResponse
{
    public string SessionId { get; set; } = string.Empty;
    public string ReplyText { get; set; } = string.Empty;
    public string? Icon { get; set; }
}

public class SendMessageRequest
{
    public string SessionId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class SendMessageResponse
{
    public string ReplyText { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
    public CharterRequest? FinalPayload { get; set; }
    public string? Icon { get; set; }
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

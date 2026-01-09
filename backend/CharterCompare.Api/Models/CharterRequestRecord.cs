namespace CharterCompare.Api.Models;

public class CharterRequestRecord
{
    public int Id { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public CharterRequest RequestData { get; set; } = null!;
    public int? RequesterId { get; set; } // Link to requester account (nullable for anonymous requests)
    public Requester? Requester { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public RequestStatus Status { get; set; } = RequestStatus.Open;
    public List<Quote> Quotes { get; set; } = new();
}

public enum RequestStatus
{
    Open,
    QuotesReceived,
    Accepted,
    Completed,
    Cancelled
}

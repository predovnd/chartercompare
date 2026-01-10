namespace CharterCompare.Api.Models;

public class CharterRequestRecord
{
    public int Id { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public CharterRequest RequestData { get; set; } = null!;
    public int? RequesterId { get; set; } // Link to requester account (nullable for anonymous requests)
    public Requester? Requester { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public RequestStatus Status { get; set; } = RequestStatus.Draft;
    public List<Quote> Quotes { get; set; } = new();
}

public enum RequestStatus
{
    Draft,           // Initial state - needs admin review
    UnderReview,     // Admin is reviewing
    Published,       // Published and operators can see it
    QuotesReceived,  // Quotes have been received
    Accepted,
    Completed,
    Cancelled
}

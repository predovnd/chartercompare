namespace CharterCompare.Api.Models;

public class Quote
{
    public int Id { get; set; }
    public int CharterRequestId { get; set; }
    public CharterRequestRecord CharterRequest { get; set; } = null!;
    public int ProviderId { get; set; }
    public Provider Provider { get; set; } = null!;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "AUD";
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public QuoteStatus Status { get; set; } = QuoteStatus.Pending;
}

public enum QuoteStatus
{
    Pending,
    Submitted,
    Accepted,
    Rejected,
    Expired
}

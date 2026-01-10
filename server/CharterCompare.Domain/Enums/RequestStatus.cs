namespace CharterCompare.Domain.Enums;

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

using CharterCompare.Application.MediatR;

namespace CharterCompare.Application.Requests.Provider;

public class SubmitQuoteCommand : IRequest<SubmitQuoteResponse>
{
    public int RequestId { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "AUD";
    public string? Notes { get; set; }
}

public class SubmitQuoteResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int? QuoteId { get; set; }
}

using CharterCompare.Application.MediatR;

namespace CharterCompare.Application.Requests.Provider;

public class GetProviderQuotesQuery : IRequest<GetProviderQuotesResponse>
{
}

public class GetProviderQuotesResponse
{
    public List<ProviderQuoteDto> Quotes { get; set; } = new();
}

public class ProviderQuoteDto
{
    public int Id { get; set; }
    public int RequestId { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public ProviderRequestDto? Request { get; set; }
}

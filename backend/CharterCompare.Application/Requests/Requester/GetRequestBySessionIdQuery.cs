using CharterCompare.Application.MediatR;
using CharterRequestDto = CharterCompare.Application.Requests.Provider.CharterRequestDto;

namespace CharterCompare.Application.Requests.Requester;

public class GetRequestBySessionIdQuery : IRequest<GetRequestBySessionIdResponse>
{
    public string SessionId { get; set; } = string.Empty;
}

public class GetRequestBySessionIdResponse
{
    public RequestBySessionIdDto? Request { get; set; }
}

public class RequestBySessionIdDto
{
    public int Id { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public Provider.CharterRequestDto RequestData { get; set; } = null!;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? QuoteDeadline { get; set; }
    public int QuoteCount { get; set; }
    public List<RequesterQuoteDto> Quotes { get; set; } = new();
    public bool IsDeadlinePassed { get; set; }
    public int HoursRemaining { get; set; }
}

public class RequesterQuoteDto
{
    public int Id { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

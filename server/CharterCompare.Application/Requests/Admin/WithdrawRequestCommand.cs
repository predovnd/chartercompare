using CharterCompare.Application.MediatR;

namespace CharterCompare.Application.Requests.Admin;

public class WithdrawRequestCommand : IRequest<WithdrawRequestResponse>
{
    public int RequestId { get; set; }
}

public class WithdrawRequestResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
}

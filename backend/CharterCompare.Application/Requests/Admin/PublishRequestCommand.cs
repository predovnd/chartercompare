using CharterCompare.Application.MediatR;

namespace CharterCompare.Application.Requests.Admin;

public class PublishRequestCommand : IRequest<PublishRequestResponse>
{
    public int RequestId { get; set; }
}

public class PublishRequestResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
}

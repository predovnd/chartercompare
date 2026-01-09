using CharterCompare.Application.MediatR;

namespace CharterCompare.Application.Requests.Admin;

public class DeleteOperatorCoverageCommand : IRequest<DeleteOperatorCoverageResponse>
{
    public int CoverageId { get; set; }
}

public class DeleteOperatorCoverageResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
}

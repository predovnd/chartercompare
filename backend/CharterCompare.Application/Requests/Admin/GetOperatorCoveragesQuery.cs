using CharterCompare.Application.MediatR;

namespace CharterCompare.Application.Requests.Admin;

public class GetOperatorCoveragesQuery : IRequest<GetOperatorCoveragesResponse>
{
    public int OperatorId { get; set; }
}

public class GetOperatorCoveragesResponse
{
    public List<OperatorCoverageDto> Coverages { get; set; } = new();
}

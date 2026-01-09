using CharterCompare.Application.MediatR;

namespace CharterCompare.Application.Requests.Admin;

public class UpdateOperatorCoverageCommand : IRequest<UpdateOperatorCoverageResponse>
{
    public int CoverageId { get; set; }
    public string BaseLocationName { get; set; } = string.Empty;
    public double CoverageRadiusKm { get; set; }
    public int MinPassengerCapacity { get; set; }
    public int MaxPassengerCapacity { get; set; }
}

public class UpdateOperatorCoverageResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public OperatorCoverageDto? Coverage { get; set; }
}

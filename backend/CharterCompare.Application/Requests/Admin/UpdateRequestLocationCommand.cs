using CharterCompare.Application.MediatR;

namespace CharterCompare.Application.Requests.Admin;

public class UpdateRequestLocationCommand : IRequest<UpdateRequestLocationResponse>
{
    public int RequestId { get; set; }
    public string LocationType { get; set; } = string.Empty; // "pickup" or "destination"
    public string LocationName { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class UpdateRequestLocationResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
}

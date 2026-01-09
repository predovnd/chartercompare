using CharterCompare.Application.MediatR;

namespace CharterCompare.Application.Requests.Admin;

public class GetAdminStatsQuery : IRequest<GetAdminStatsResponse>
{
}

public class GetAdminStatsResponse
{
    public int TotalRequests { get; set; }
    public int OpenRequests { get; set; }
    public int TotalQuotes { get; set; }
    public int TotalOperators { get; set; }
    public int TotalRequesters { get; set; }
}

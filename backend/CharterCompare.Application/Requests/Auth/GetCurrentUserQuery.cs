using CharterCompare.Application.MediatR;

namespace CharterCompare.Application.Requests.Auth;

public class GetCurrentUserQuery : IRequest<GetCurrentUserResponse>
{
}

public class GetCurrentUserResponse
{
    public bool IsAuthenticated { get; set; }
    public int? Id { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? UserType { get; set; } // "operator", "requester", "admin"
    public bool IsAdmin { get; set; }
}

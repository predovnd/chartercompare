using CharterCompare.Application.MediatR;

namespace CharterCompare.Application.Requests.Admin;

public class GetAdminUsersQuery : IRequest<GetAdminUsersResponse>
{
}

public class GetAdminUsersResponse
{
    public List<UserDto> Users { get; set; } = new();
}

public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? Phone { get; set; }
    public string ExternalProvider { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int QuoteCount { get; set; }
    public int RequestCount { get; set; }
    public string UserType { get; set; } = string.Empty; // "operator" or "requester"
}

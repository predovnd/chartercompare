using CharterCompare.Application.MediatR;

namespace CharterCompare.Application.Requests.Auth;

public class RequesterLoginCommand : IRequest<RequesterLoginResponse>
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RequesterLoginResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public RequesterInfo? Requester { get; set; }
}

public class RequesterInfo
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

using CharterCompare.Application.MediatR;

namespace CharterCompare.Application.Requests.Auth;

public class RequesterRegisterCommand : IRequest<RequesterRegisterResponse>
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

public class RequesterRegisterResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public RequesterInfo? Requester { get; set; }
}

using CharterCompare.Application.MediatR;
using CharterCompare.Domain.Enums;

namespace CharterCompare.Application.Requests.Auth;

public class RequesterRegisterCommand : IRequest<RequesterRegisterResponse>
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Phone { get; set; }
    // Note: Attributes default to Individual for requesters (set in handler)
    // Only admins can change attributes via admin endpoint
}

public class RequesterRegisterResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public RequesterInfo? Requester { get; set; }
}

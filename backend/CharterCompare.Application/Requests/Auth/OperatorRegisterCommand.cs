using CharterCompare.Application.MediatR;

namespace CharterCompare.Application.Requests.Auth;

public class OperatorRegisterCommand : IRequest<OperatorRegisterResponse>
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? Phone { get; set; }
}

public class OperatorRegisterResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public OperatorInfo? Operator { get; set; }
}

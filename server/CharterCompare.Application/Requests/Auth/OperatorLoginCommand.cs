using CharterCompare.Application.MediatR;

namespace CharterCompare.Application.Requests.Auth;

public class OperatorLoginCommand : IRequest<OperatorLoginResponse>
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class OperatorLoginResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public OperatorInfo? Operator { get; set; }
}

public class OperatorInfo
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public bool IsAdmin { get; set; }
}

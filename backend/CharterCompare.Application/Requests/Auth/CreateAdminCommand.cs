using CharterCompare.Application.MediatR;

namespace CharterCompare.Application.Requests.Auth;

public class CreateAdminCommand : IRequest<CreateAdminResponse>
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string? CompanyName { get; set; }
}

public class CreateAdminResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
}

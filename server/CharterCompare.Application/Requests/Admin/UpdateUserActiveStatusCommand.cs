using CharterCompare.Application.MediatR;

namespace CharterCompare.Application.Requests.Admin;

public class UpdateUserActiveStatusCommand : IRequest<UpdateUserActiveStatusResponse>
{
    public int UserId { get; set; }
    public bool IsActive { get; set; }
}

public class UpdateUserActiveStatusResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
}

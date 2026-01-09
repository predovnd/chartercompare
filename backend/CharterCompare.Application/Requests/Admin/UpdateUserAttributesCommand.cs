using CharterCompare.Application.MediatR;
using CharterCompare.Domain.Enums;

namespace CharterCompare.Application.Requests.Admin;

public class UpdateUserAttributesCommand : IRequest<UpdateUserAttributesResponse>
{
    public int UserId { get; set; }
    public List<UserAttributeType> Attributes { get; set; } = new();
    public string? CompanyName { get; set; } // For Business attribute
}

public class UpdateUserAttributesResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
}

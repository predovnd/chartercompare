using CharterCompare.Domain.Enums;

namespace CharterCompare.Domain.Entities;

public class UserAttribute
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public UserAttributeType AttributeType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

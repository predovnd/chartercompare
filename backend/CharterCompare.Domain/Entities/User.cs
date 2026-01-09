using CharterCompare.Domain.Enums;

namespace CharterCompare.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? CompanyName { get; set; } // For operators/admins and business requesters
    public string ExternalId { get; set; } = string.Empty; // Google ID or other OAuth provider ID
    public string ExternalProvider { get; set; } = string.Empty; // "Google", "Email", etc.
    public string? PasswordHash { get; set; } // For email/password authentication
    public UserRole Role { get; set; } = UserRole.Requester;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<Quote> Quotes { get; set; } = new List<Quote>();
    public ICollection<CharterRequestRecord> Requests { get; set; } = new List<CharterRequestRecord>();
    public ICollection<UserAttribute> Attributes { get; set; } = new List<UserAttribute>();
    public ICollection<OperatorCoverage> OperatorCoverages { get; set; } = new List<OperatorCoverage>();

    // Helper properties
    public bool IsAdmin => Role == UserRole.Admin;
    public bool IsOperator => Role == UserRole.Operator || Role == UserRole.Admin;
    public bool IsRequester => Role == UserRole.Requester;
}

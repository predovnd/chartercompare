namespace CharterCompare.Api.Models;

public class Provider
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? Phone { get; set; }
    public string ExternalId { get; set; } = string.Empty; // Google ID or other OAuth provider ID
    public string ExternalProvider { get; set; } = string.Empty; // "Google", "Email", etc.
    public string? PasswordHash { get; set; } // For email/password authentication
    public bool IsAdmin { get; set; } = false; // Admin flag
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;
}

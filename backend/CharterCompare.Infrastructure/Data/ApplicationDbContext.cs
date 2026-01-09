using CharterCompare.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CharterCompare.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<CharterRequestRecord> CharterRequests { get; set; }
    public DbSet<Quote> Quotes { get; set; }
    public DbSet<UserAttribute> UserAttributes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => new { e.ExternalId, e.ExternalProvider }).IsUnique();
            entity.HasMany(e => e.Attributes)
                .WithOne(a => a.User)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserAttribute>(entity =>
        {
            entity.ToTable("UserAttributes");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.AttributeType }).IsUnique(); // Prevent duplicate attributes
        });

        modelBuilder.Entity<CharterRequestRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RequestData)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null!),
                    v => System.Text.Json.JsonSerializer.Deserialize<CharterRequest>(v, (System.Text.Json.JsonSerializerOptions?)null!)!);
            entity.HasOne(e => e.Requester)
                .WithMany(r => r.Requests)
                .HasForeignKey(e => e.RequesterId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Quote>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.CharterRequest)
                .WithMany(r => r.Quotes)
                .HasForeignKey(e => e.CharterRequestId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Provider)
                .WithMany(o => o.Quotes)
                .HasForeignKey(e => e.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

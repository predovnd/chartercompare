using CharterCompare.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CharterCompare.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Operator> Providers { get; set; } // Keep table name as Providers for backward compatibility
    public DbSet<Requester> Requesters { get; set; }
    public DbSet<CharterRequestRecord> CharterRequests { get; set; }
    public DbSet<Quote> Quotes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Operator>(entity =>
        {
            entity.ToTable("Providers"); // Keep table name for backward compatibility
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => new { e.ExternalId, e.ExternalProvider }).IsUnique();
        });

        modelBuilder.Entity<Requester>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => new { e.ExternalId, e.ExternalProvider }).IsUnique();
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

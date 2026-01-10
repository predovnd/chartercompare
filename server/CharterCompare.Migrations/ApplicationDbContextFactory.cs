using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace CharterCompare.Migrations;

/// <summary>
/// Factory for creating ApplicationDbContext instances during design-time operations (e.g., migrations).
/// This factory is located in the dedicated migrations project to decouple migrations from the web host.
/// This is used by EF Core tooling (dotnet ef migrations, dotnet ef database update, etc.)
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<CharterCompare.Infrastructure.Data.ApplicationDbContext>
{
    public CharterCompare.Infrastructure.Data.ApplicationDbContext CreateDbContext(string[] args)
    {
        // Build configuration from appsettings.json in this migrations project
        var basePath = GetBasePath();

        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        var configuration = configurationBuilder.Build();

        // Get connection string with priority:
        // 1. CHARCOMPARE_MIGRATIONS_CONNECTION environment variable (for migrations - highest priority)
        // 2. ConnectionStrings:DefaultConnection from appsettings.json in migrations project
        // 3. ConnectionStrings__DefaultConnection environment variable (ASP.NET Core format)
        var connectionString = Environment.GetEnvironmentVariable("CHARCOMPARE_MIGRATIONS_CONNECTION")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string is not configured. " +
                "Please set one of the following: " +
                "CHARCOMPARE_MIGRATIONS_CONNECTION environment variable, " +
                "ConnectionStrings:DefaultConnection in appsettings.json, or " +
                "ConnectionStrings__DefaultConnection environment variable.");
        }

        // Build DbContextOptions - this does NOT connect to the database
        // Connection only happens when methods like SaveChanges(), Migrate(), etc. are called
        var optionsBuilder = new DbContextOptionsBuilder<CharterCompare.Infrastructure.Data.ApplicationDbContext>();
        optionsBuilder.UseSqlServer(
            connectionString,
            sqlServerOptions => sqlServerOptions.MigrationsAssembly("CharterCompare.Migrations"));

        return new CharterCompare.Infrastructure.Data.ApplicationDbContext(optionsBuilder.Options);
    }

    private static string GetBasePath()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        
        // Try common relative paths to find the migrations project directory (where appsettings.json is located)
        var pathsToTry = new[]
        {
            // If running from migrations project directory
            currentDirectory,
            // If running from server directory
            Path.Combine(currentDirectory, "CharterCompare.Migrations"),
            // If running from repo root
            Path.Combine(currentDirectory, "server/CharterCompare.Migrations"),
            Path.Combine(currentDirectory, "../server/CharterCompare.Migrations"),
            // Fallback: try to find the migrations project by looking for appsettings.json
            Path.GetDirectoryName(typeof(ApplicationDbContextFactory).Assembly.Location) ?? currentDirectory
        };

        foreach (var path in pathsToTry)
        {
            var fullPath = Path.GetFullPath(path);
            if (Directory.Exists(fullPath))
            {
                var appsettingsPath = Path.Combine(fullPath, "appsettings.json");
                if (File.Exists(appsettingsPath))
                {
                    return fullPath;
                }
            }
        }

        // Fallback to current directory (will use environment variables or default connection string)
        return currentDirectory;
    }
}

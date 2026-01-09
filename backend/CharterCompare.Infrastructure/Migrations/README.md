# Database Migration System

## Overview

The migration system has been moved to the Infrastructure layer to make it database-provider specific and easily switchable. This allows you to change database providers (SQL Server, PostgreSQL, MySQL, etc.) without modifying the API layer.

## Architecture

### Interface
- `IDatabaseMigrator` - Abstract interface for database migrations
- Located in: `CharterCompare.Infrastructure/Migrations/IDatabaseMigrator.cs`

### Implementations
- `SqlServerDatabaseMigrator` - SQL Server-specific migration implementation
- Located in: `CharterCompare.Infrastructure/Migrations/SqlServerDatabaseMigrator.cs`

## Current Implementation: SQL Server

The `SqlServerDatabaseMigrator` handles:
1. **Database Creation**: Creates the database if it doesn't exist
2. **Schema Creation**: Creates the `Users` table and other tables based on DbContext
3. **Data Migration**: Automatically migrates data from old schema (Providers/Requesters) to new unified schema (Users)
4. **Foreign Key Updates**: Updates foreign keys in Quotes and CharterRequests tables

## Switching Database Providers

To switch to a different database provider (e.g., PostgreSQL, MySQL):

### Step 1: Update DbContext Configuration

In `Program.cs`, change the database provider:

```csharp
// For PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// For MySQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
```

### Step 2: Create New Migrator Implementation

Create a new migrator class in `CharterCompare.Infrastructure/Migrations/`:

```csharp
// Example: PostgreSqlDatabaseMigrator.cs
public class PostgreSqlDatabaseMigrator : IDatabaseMigrator
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<PostgreSqlDatabaseMigrator> _logger;

    public PostgreSqlDatabaseMigrator(ApplicationDbContext dbContext, ILogger<PostgreSqlDatabaseMigrator> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        // PostgreSQL-specific migration logic
        // Use PostgreSQL SQL syntax instead of SQL Server syntax
    }
}
```

### Step 3: Update Dependency Injection

In `Program.cs`, change the migrator registration:

```csharp
// For PostgreSQL
builder.Services.AddScoped<IDatabaseMigrator, PostgreSqlDatabaseMigrator>();

// For MySQL
builder.Services.AddScoped<IDatabaseMigrator, MySqlDatabaseMigrator>();
```

### Step 4: Update SQL Syntax

The new migrator implementation should use the target database's SQL syntax:

- **SQL Server**: `SELECT TOP 1`, `NVARCHAR`, `DATETIME2`, `IDENTITY(1,1)`
- **PostgreSQL**: `SELECT ... LIMIT 1`, `VARCHAR`, `TIMESTAMP`, `SERIAL` or `GENERATED ALWAYS AS IDENTITY`
- **MySQL**: `SELECT ... LIMIT 1`, `VARCHAR`, `DATETIME`, `AUTO_INCREMENT`

## Key Differences Between Providers

### Table Existence Check
- **SQL Server**: `SELECT TOP 1 Id FROM TableName WHERE 1=0`
- **PostgreSQL**: `SELECT 1 FROM information_schema.tables WHERE table_name = 'tablename'`
- **MySQL**: `SELECT 1 FROM information_schema.tables WHERE table_name = 'tablename'`

### Table Creation
- **SQL Server**: Uses `IF NOT EXISTS` with `sys.tables`
- **PostgreSQL**: Uses `CREATE TABLE IF NOT EXISTS`
- **MySQL**: Uses `CREATE TABLE IF NOT EXISTS`

### Data Types
- **SQL Server**: `NVARCHAR`, `DATETIME2`, `BIT`, `INT IDENTITY`
- **PostgreSQL**: `VARCHAR`, `TIMESTAMP`, `BOOLEAN`, `SERIAL` or `INT GENERATED ALWAYS AS IDENTITY`
- **MySQL**: `VARCHAR`, `DATETIME`, `TINYINT(1)`, `INT AUTO_INCREMENT`

## Benefits of This Architecture

1. **Separation of Concerns**: Migration logic is isolated in Infrastructure layer
2. **Database Agnostic API**: API layer doesn't know about database-specific details
3. **Easy Switching**: Change database provider by swapping implementations
4. **Testability**: Can create mock migrators for testing
5. **Maintainability**: Each database provider has its own implementation

## Future Enhancements

Consider adding:
- EF Core Migrations support (instead of raw SQL)
- Migration versioning/tracking
- Rollback capabilities
- Multiple database provider support in same application

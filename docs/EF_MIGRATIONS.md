# EF Core Code-First Migrations Guide

## Overview

This project uses **EF Core Code-First Migrations** with SQL Server. The `ApplicationDbContext` and entity models in the Infrastructure layer are the source of truth for the database schema.

**Key Points:**
- Migrations are stored in `server/CharterCompare.Migrations/Migrations/` (dedicated migrations project)
- DbContext: `CharterCompare.Infrastructure.Data.ApplicationDbContext`
- Database Provider: SQL Server
- Migrations are generated from the DbContext and committed to the repository
- **Local dotnet-ef tool**: Version 8.0.0 (matches EF Core 8.0.0) - run `dotnet tool restore` first
- **Initial Migration**: `InitialCreate` migration has been created (timestamp: 20260110020831)

## Quick Reference

**All commands must be run from the workspace root and require `dotnet tool restore` first.**

### Common Commands

```bash
# 1. Restore local tools (REQUIRED before any EF Core commands)
dotnet tool restore

# 2. Show DbContext information (verbose)
dotnet ef dbcontext info \
  --context ApplicationDbContext \
  --startup-project server/CharterCompare.Migrations/CharterCompare.Migrations.csproj \
  --project server/CharterCompare.Migrations/CharterCompare.Migrations.csproj \
  --verbose

# 3. List all migrations and their status
dotnet ef migrations list \
  --context ApplicationDbContext \
  --startup-project server/CharterCompare.Migrations/CharterCompare.Migrations.csproj \
  --project server/CharterCompare.Migrations/CharterCompare.Migrations.csproj

# 4. Add a new migration
dotnet ef migrations add <MigrationName> \
  --context ApplicationDbContext \
  --startup-project server/CharterCompare.Migrations/CharterCompare.Migrations.csproj \
  --project server/CharterCompare.Migrations/CharterCompare.Migrations.csproj

# 5. Apply migrations to database (DO NOT run until ready)
dotnet ef database update \
  --context ApplicationDbContext \
  --startup-project server/CharterCompare.Migrations/CharterCompare.Migrations.csproj \
  --project server/CharterCompare.Migrations/CharterCompare.Migrations.csproj
```

## Prerequisites

1. **.NET 8.0 SDK** installed

2. **EF Core Tools (Local Tool)** - This repository uses a local dotnet tool manifest to ensure version compatibility:
   ```bash
   # Restore the local dotnet-ef tool (version 8.0.0 matching EF Core 8.0.0)
   dotnet tool restore
   ```
   
   **Important**: Always run `dotnet tool restore` before using EF Core commands. This ensures you're using the correct version (8.0.0) that matches the EF Core packages in this repository.
   
   The tool manifest is located at `.config/dotnet-tools.json` and is committed to source control.
   
   **Why local tools?** This prevents version mismatches where a globally installed dotnet-ef tool (e.g., version 10.0.1) would conflict with EF Core 8.0.0 packages, causing runtime errors like missing `System.Runtime 10.0.0.0`.

3. **SQL Server** or **LocalDB** available for development

## Helper Scripts

Convenient helper scripts are provided in `server/` directory:

### PowerShell (Windows)
```powershell
# From server directory
.\migrate.ps1 add AddEmailToUser
.\migrate.ps1 update
.\migrate.ps1 list
```

### Bash (Linux/macOS)
```bash
# From server directory
./migrate.sh add AddEmailToUser
./migrate.sh update
./migrate.sh list
```

For more information, run:
```bash
./migrate.sh help
# or
.\migrate.ps1 help
```

## Creating Migrations

> **⚠️ IMPORTANT: Before Running Any EF Core Commands**  
> **Always restore local tools first:**
> ```bash
> dotnet tool restore
> ```
> This ensures you're using dotnet-ef 8.0.0 which matches the EF Core 8.0.0 packages in this repository.

> **⚠️ IMPORTANT: Deterministic Migration Commands**  
> All EF Core migration commands MUST use explicit `--context`, `--startup-project`, and `--project` parameters to ensure determinism.  
> This is especially critical for automated tools like Cursor/AI assistants.

### Exact Command Format for Cursor/AI Tools

**To create a new migration, Cursor should run these exact commands:**

```bash
# Step 1: ALWAYS restore local tools first (REQUIRED)
dotnet tool restore

# Step 2: Create the migration
dotnet ef migrations add <MigrationName> \
  --context ApplicationDbContext \
  --startup-project server/CharterCompare.Migrations/CharterCompare.Migrations.csproj \
  --project server/CharterCompare.Migrations/CharterCompare.Migrations.csproj
```

**Replace `<MigrationName>` with a descriptive name** (e.g., `AddEmailColumnToRequests`, `UpdateUserTableIndexes`)

**All parameters are required:**
- **First**: `dotnet tool restore` - Ensures the correct dotnet-ef 8.0.0 version is used (matches EF Core 8.0.0)
- `--context ApplicationDbContext` - Explicitly specifies the DbContext (there is only one: `ApplicationDbContext`)
- `--startup-project server/CharterCompare.Migrations/CharterCompare.Migrations.csproj` - Path to migrations project (has its own appsettings.json and factory)
- `--project server/CharterCompare.Migrations/CharterCompare.Migrations.csproj` - Path to migrations project (where migrations are stored)

### DbContext Configuration

**DbContext Classes:**
- **Main DbContext**: `CharterCompare.Infrastructure.Data.ApplicationDbContext`
  - File: `server/CharterCompare.Infrastructure/Data/ApplicationDbContext.cs`
  - Full namespace: `CharterCompare.Infrastructure.Data.ApplicationDbContext`

**Design-Time Factory:**
- `ApplicationDbContextFactory` is located in the dedicated migrations project
- File: `server/CharterCompare.Migrations/ApplicationDbContextFactory.cs`
- Returns: `ApplicationDbContext`
- The migrations project has its own `appsettings.json` for connection string configuration

### Deterministic Migration Command

**All migrations MUST be created using the exact command format below** to ensure determinism:

**Step 1: Restore local tools (REQUIRED)**
```bash
dotnet tool restore
```

**Step 2: Create migration**
```bash
dotnet ef migrations add <MigrationName> \
  --context ApplicationDbContext \
  --startup-project server/CharterCompare.Migrations/CharterCompare.Migrations.csproj \
  --project server/CharterCompare.Migrations/CharterCompare.Migrations.csproj
```

**Parameters:**
- `--context ApplicationDbContext` - Explicitly specifies the DbContext class name
- `--startup-project` (or `-s`) - Path to the migrations project (has its own appsettings.json and factory)
- `--project` (or `-p`) - Path to the migrations project (where migrations will be stored)

### First Migration (Initial Schema)

**Status**: The `InitialCreate` migration has already been created for this repository.

**Migration Details:**
- **Name**: `InitialCreate`
- **Timestamp**: `20260110020831`
- **Location**: `server/CharterCompare.Migrations/Migrations/`
- **Files Generated**:
  - `20260110020831_InitialCreate.cs` - Migration Up/Down methods
  - `20260110020831_InitialCreate.Designer.cs` - Migration metadata
  - `ApplicationDbContextModelSnapshot.cs` - Current model snapshot

**To create the initial migration for a new setup:**

```bash
# Step 1: Restore local tools (REQUIRED)
dotnet tool restore

# Step 2: From workspace root, create the initial migration
dotnet ef migrations add InitialCreate \
  --context ApplicationDbContext \
  --startup-project server/CharterCompare.Migrations/CharterCompare.Migrations.csproj \
  --project server/CharterCompare.Migrations/CharterCompare.Migrations.csproj
```

**Note**: The `InitialCreate` migration contains the complete database schema including all entities:
- Users, UserAttributes
- CharterRequests
- Quotes
- OperatorCoverages

### Subsequent Migrations

When you modify entity models or DbContext configuration:

```bash
# Step 1: Restore local tools (REQUIRED if not already done)
dotnet tool restore

# Step 2: From workspace root - ALWAYS use this exact format
dotnet ef migrations add <MigrationName> \
  --context ApplicationDbContext \
  --startup-project server/CharterCompare.Migrations/CharterCompare.Migrations.csproj \
  --project server/CharterCompare.Migrations/CharterCompare.Migrations.csproj

# Example:
dotnet ef migrations add AddEmailColumnToRequests \
  --context ApplicationDbContext \
  --startup-project server/CharterCompare.Migrations/CharterCompare.Migrations.csproj \
  --project server/CharterCompare.Migrations/CharterCompare.Migrations.csproj
```

**Migration Naming Convention:**
- Use descriptive names: `AddEmailColumnToRequests`, `UpdateUserTableIndexes`
- Use PascalCase
- Be specific about what changed

## Applying Migrations Locally

### Development Environment

**Option 1: Auto-apply on startup (default in Development)**
- Set `Database:AutoMigrate=true` in `appsettings.Development.json`
- Or rely on default behavior (auto-migrate is enabled in Development by default)
- Run the application: `dotnet run --project server/CharterCompare.Api`

**Option 2: Manual migration**
```bash
# Step 1: Restore local tools (REQUIRED)
dotnet tool restore

# Step 2: From workspace root - use explicit parameters for determinism
dotnet ef database update \
  --context ApplicationDbContext \
  --startup-project server/CharterCompare.Migrations/CharterCompare.Migrations.csproj \
  --project server/CharterCompare.Migrations/CharterCompare.Migrations.csproj
```

### Check Migration Status

```bash
# Step 1: Restore local tools (REQUIRED)
dotnet tool restore

# Step 2: List all migrations and their status
# From workspace root - use explicit parameters for determinism
dotnet ef migrations list \
  --context ApplicationDbContext \
  --startup-project server/CharterCompare.Migrations/CharterCompare.Migrations.csproj \
  --project server/CharterCompare.Migrations/CharterCompare.Migrations.csproj
```

### Rollback Migrations

```bash
# Step 1: Restore local tools (REQUIRED)
dotnet tool restore

# Step 2: Rollback to a specific migration
# From workspace root - use explicit parameters for determinism
dotnet ef database update <PreviousMigrationName> \
  --context ApplicationDbContext \
  --startup-project server/CharterCompare.Migrations/CharterCompare.Migrations.csproj \
  --project server/CharterCompare.Migrations/CharterCompare.Migrations.csproj

# Example: Rollback to InitialCreate
dotnet ef database update InitialCreate \
  --context ApplicationDbContext \
  --startup-project server/CharterCompare.Migrations/CharterCompare.Migrations.csproj \
  --project server/CharterCompare.Migrations/CharterCompare.Migrations.csproj
```

## Configuration

### Connection String

Connection strings are configured in:

1. **Local Development**: `server/CharterCompare.Api/appsettings.Development.json`
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CharterCompare;Trusted_Connection=true;TrustServerCertificate=true;MultipleActiveResultSets=true"
     },
     "Database": {
       "AutoMigrate": true
     }
   }
   ```

2. **Production/Staging**: Azure App Service Configuration → Connection Strings
   - Name: `DefaultConnection`
   - Value: Your SQL Server connection string

### Auto-Migration Settings

Auto-migration can be controlled via configuration:

```json
{
  "Database": {
    "AutoMigrate": true  // true = auto-apply on startup, false = manual only
  }
}
```

**Defaults:**
- **Development**: `true` (auto-apply on startup)
- **Production**: `false` (manual migration required)

**Important**: In production, auto-migration should be disabled and migrations should be applied manually or via CI/CD pipeline.

## Workflow

### Making Schema Changes

1. **Modify Entity Models** or **DbContext** configuration
   ```csharp
   // Example: Add property to User entity
   public class User
   {
       // ... existing properties ...
       public string? PhoneNumber { get; set; }  // New property
   }
   ```

2. **Create Migration**
   ```bash
   # Step 1: Restore local tools (REQUIRED)
   dotnet tool restore
   
   # Step 2: Create the migration
   dotnet ef migrations add AddPhoneNumberToUser \
     --context ApplicationDbContext \
     --startup-project server/CharterCompare.Migrations/CharterCompare.Migrations.csproj \
     --project server/CharterCompare.Migrations/CharterCompare.Migrations.csproj
   ```

3. **Review Migration File**
   - Check `server/CharterCompare.Migrations/Migrations/<timestamp>_AddPhoneNumberToUser.cs`
   - Verify the Up() and Down() methods are correct
   - If needed, customize the migration for data transformations

4. **Test Locally**
   ```bash
   # Step 1: Restore local tools (REQUIRED)
   dotnet tool restore
   
   # Step 2: Apply migration locally - use explicit parameters for determinism
   dotnet ef database update \
     --context ApplicationDbContext \
     --startup-project server/CharterCompare.Migrations/CharterCompare.Migrations.csproj \
     --project server/CharterCompare.Migrations/CharterCompare.Migrations.csproj
   
   # Step 3: Run application and test
   dotnet run --project server/CharterCompare.Api
   ```

5. **Commit Migration**
   ```bash
   git add server/CharterCompare.Migrations/Migrations/
   git commit -m "Add PhoneNumber property to User entity"
   ```

6. **Push and Deploy**
   - Migrations are applied automatically in dev/staging (via CI/CD)
   - For production: apply manually or via approved CI/CD step

## CI/CD Integration

### Development/Staging Environments

Migrations are automatically applied during deployment via the CI/CD pipeline:

1. **GitHub Actions** runs `dotnet ef database update` before deploying
2. Migrations are applied to the target database
3. Application is deployed

### Production Environment

**Recommended Approach:**
1. Migrations are **NOT** auto-applied in production by default
2. Manual approval step required in CI/CD
3. Or manually run: `dotnet ef database update` against production database

**To enable auto-migration in production (NOT recommended without review):**
- Set `Database:AutoMigrate=true` in Azure App Service Application Settings
- Or use Azure App Service deployment slots for zero-downtime deployments

## Troubleshooting

### Migration Fails: "No migrations found"

**Solution:**
1. Ensure migrations exist in `server/CharterCompare.Migrations/Migrations/`
2. Verify the migration assembly is correctly specified in `Program.cs`:
   ```csharp
   options.UseSqlServer(connectionString, 
       sqlServerOptions => sqlServerOptions.MigrationsAssembly("CharterCompare.Migrations"));
   ```
3. Ensure you've run `dotnet tool restore` to use the correct dotnet-ef version

### Migration Fails: "Connection String not found"

**Solution:**
1. Check `server/CharterCompare.Migrations/appsettings.json` has `ConnectionStrings:DefaultConnection`
2. Or set environment variable: `CHARCOMPARE_MIGRATIONS_CONNECTION` (highest priority)
3. Or set environment variable: `ConnectionStrings__DefaultConnection`
4. The `ApplicationDbContextFactory` in the migrations project handles design-time configuration

### Migration Fails: "Tool version mismatch" or "System.Runtime 10.0.0.0 not found"

**Solution:**
1. This indicates you're using a globally installed dotnet-ef tool (e.g., version 10.0.1) instead of the local tool (version 8.0.0)
2. **Always run `dotnet tool restore` first** to ensure the local tool is used
3. Verify the tool manifest exists: `.config/dotnet-tools.json`
4. Check the installed version: `dotnet tool list` should show `dotnet-ef 8.0.0` in the local tools
5. Use `dotnet ef` (which will use the local tool) instead of calling `dotnet-ef` directly

### Migration Conflicts

If you have conflicting migrations (e.g., two developers created migrations simultaneously):

1. **Option 1: Merge migrations manually**
   - Keep both migrations
   - Ensure they don't conflict
   - Apply both

2. **Option 2: Remove and recreate**
   - Remove the conflicting migration file
   - Create a new migration that includes both changes

### Database Schema Drift

If the database schema doesn't match migrations:

1. **For Development**: Drop and recreate
   ```sql
   DROP DATABASE CharterCompare;
   ```
   Then run:
   ```bash
   # Step 1: Restore local tools (REQUIRED)
   dotnet tool restore
   
   # Step 2: Apply all migrations
   dotnet ef database update \
     --context ApplicationDbContext \
     --startup-project server/CharterCompare.Migrations/CharterCompare.Migrations.csproj \
     --project server/CharterCompare.Migrations/CharterCompare.Migrations.csproj
   ```

2. **For Production**: Create a migration that brings schema in line
   ```bash
   # Step 1: Restore local tools (REQUIRED)
   dotnet tool restore
   
   # Step 2: Create sync migration
   dotnet ef migrations add SyncSchema \
     --context ApplicationDbContext \
     --startup-project server/CharterCompare.Migrations/CharterCompare.Migrations.csproj \
     --project server/CharterCompare.Migrations/CharterCompare.Migrations.csproj
   ```
   Review the migration carefully before applying.

## Best Practices

1. **Always review migration files** before committing
2. **Test migrations locally** before pushing
3. **Use descriptive migration names** that explain what changed
4. **Don't edit applied migrations** - create a new migration instead
5. **Keep migrations small and focused** - one logical change per migration
6. **For data migrations**, use `Sql()` method in migrations:
   ```csharp
   migrationBuilder.Sql("UPDATE Users SET PhoneNumber = '' WHERE PhoneNumber IS NULL");
   ```
7. **Backup production databases** before applying migrations
8. **Apply migrations during low-traffic periods** in production
9. **Monitor migration logs** in Application Insights or logs

## Legacy Migrator (Removed)

✅ **Note**: The obsolete `SqlServerDatabaseMigrator` class and `IDatabaseMigrator` interface have been removed. The project now exclusively uses EF Core Migrations.

## Additional Resources

- [EF Core Migrations Documentation](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [EF Core Tools Reference](https://learn.microsoft.com/en-us/ef/core/cli/dotnet-ef)
- [SQL Server Provider](https://learn.microsoft.com/en-us/ef/core/providers/sql-server/)

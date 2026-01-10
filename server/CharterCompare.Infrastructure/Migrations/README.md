# Database Migration System

## Overview

This project now uses **EF Core Migrations** exclusively for database schema management. The legacy custom migration system (`IDatabaseMigrator` and `SqlServerDatabaseMigrator`) has been removed.

## Current Implementation: EF Core Migrations

EF Core Migrations are stored in the `CharterCompare.Migrations` project and managed using standard EF Core tooling commands:

- `dotnet ef migrations add <MigrationName>` - Create a new migration
- `dotnet ef migrations list` - List all migrations
- `dotnet ef database update` - Apply pending migrations
- `dotnet ef migrations script` - Generate SQL script for migrations

For detailed migration documentation, see:
- `docs/EF_MIGRATIONS.md` - Complete EF Core Migrations guide
- `docs/EF_MIGRATIONS_HEALTHCHECK.md` - Health check and best practices

## Legacy Custom Migration System (Removed)

The previous custom migration system has been removed. This README section is kept for historical reference only.

## Switching Database Providers (EF Core Migrations)

With EF Core Migrations, switching database providers is straightforward:

### Step 1: Install Database Provider NuGet Package

```bash
# For PostgreSQL
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL

# For MySQL
dotnet add package Pomelo.EntityFrameworkCore.MySql
```

### Step 2: Update DbContext Configuration

In `Program.cs`, change the database provider:

```csharp
// For PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString,
        npgsqlOptions => npgsqlOptions.MigrationsAssembly("CharterCompare.Migrations")));

// For MySQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
        mySqlOptions => mySqlOptions.MigrationsAssembly("CharterCompare.Migrations")));
```

### Step 3: Create New Migration for Target Database

```bash
dotnet ef migrations add InitialMigrationForPostgreSQL --project CharterCompare.Migrations
```

EF Core will automatically generate provider-specific SQL when you apply migrations.

### Benefits of EF Core Migrations

1. **Standard Tooling**: Uses official EF Core migration commands
2. **Provider Agnostic**: EF Core handles SQL differences automatically
3. **Version Control**: All migrations are stored as code files
4. **Rollback Support**: Can revert to previous migrations
5. **SQL Generation**: Can generate SQL scripts for review before applying

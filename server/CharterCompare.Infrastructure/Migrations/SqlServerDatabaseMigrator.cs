using CharterCompare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace CharterCompare.Infrastructure.Migrations;

public class SqlServerDatabaseMigrator : IDatabaseMigrator
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<SqlServerDatabaseMigrator> _logger;

    public SqlServerDatabaseMigrator(ApplicationDbContext dbContext, ILogger<SqlServerDatabaseMigrator> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // For SQL Server, use EnsureCreated() which will create the schema based on DbContext
            // In production, you should use EF Core Migrations instead
            
            // First, ensure database can connect
            if (!await _dbContext.Database.CanConnectAsync(cancellationToken))
            {
                _logger.LogInformation("Database does not exist. Creating database...");
                await _dbContext.Database.EnsureCreatedAsync(cancellationToken);
                await EnsureUserAttributesTableExistsAsync(cancellationToken);
                await EnsureOperatorCoveragesTableExistsAsync(cancellationToken);
                _logger.LogInformation("Database created successfully.");
                return;
            }

            // Check if Users table exists (new unified schema)
            var usersTableExists = await CheckTableExistsAsync("Users", cancellationToken);

            if (!usersTableExists)
            {
                _logger.LogInformation("Users table does not exist.");
                
                // Check if old schema exists (Providers/Requesters tables)
                var oldSchemaExists = await CheckTableExistsAsync("Providers", cancellationToken);

                if (oldSchemaExists)
                {
                    _logger.LogInformation("Old schema (Providers/Requesters) detected.");
                    await CreateUsersTableAsync(cancellationToken);
                    await EnsureUserAttributesTableExistsAsync(cancellationToken);
                    await EnsureOperatorCoveragesTableExistsAsync(cancellationToken);
                    await DropOldTypeColumnsAsync(cancellationToken);
                    await EnsureRawJsonPayloadColumnExistsAsync(cancellationToken);
                    await EnsureQuoteDeadlineColumnExistsAsync(cancellationToken);
                    await EnsureEmailColumnExistsAsync(cancellationToken);
                    await MigrateDataFromOldSchemaAsync(cancellationToken);
                }
                else
                {
                    _logger.LogInformation("Creating new unified schema...");
                    await _dbContext.Database.EnsureCreatedAsync(cancellationToken);
                    
                    // Ensure UserAttributes table exists (EnsureCreated should create it, but verify)
                    await EnsureUserAttributesTableExistsAsync(cancellationToken);
                    
                    // Ensure OperatorCoverages table exists
                    await EnsureOperatorCoveragesTableExistsAsync(cancellationToken);
                    
                    // Remove old columns if they exist
                    await DropOldTypeColumnsAsync(cancellationToken);
                    
                    // Ensure RawJsonPayload column exists
                    await EnsureRawJsonPayloadColumnExistsAsync(cancellationToken);
                    
                    // Ensure QuoteDeadline column exists
                    await EnsureQuoteDeadlineColumnExistsAsync(cancellationToken);
                    
                    // Ensure Email column exists
                    await EnsureEmailColumnExistsAsync(cancellationToken);
                    
                    _logger.LogInformation("Database schema created successfully.");
                }
            }
            else
            {
                _logger.LogInformation("Users table exists.");
                
                // Always ensure UserAttributes table exists when Users table exists
                await EnsureUserAttributesTableExistsAsync(cancellationToken);
                
                // Ensure OperatorCoverages table exists
                await EnsureOperatorCoveragesTableExistsAsync(cancellationToken);
                
                // Remove old OperatorType and RequesterType columns if they exist
                await DropOldTypeColumnsAsync(cancellationToken);
                
                // Ensure RawJsonPayload column exists in CharterRequests table
                await EnsureRawJsonPayloadColumnExistsAsync(cancellationToken);
                
                // Ensure QuoteDeadline column exists in CharterRequests table
                await EnsureQuoteDeadlineColumnExistsAsync(cancellationToken);
                
                // Ensure Email column exists in CharterRequests table
                await EnsureEmailColumnExistsAsync(cancellationToken);
                
                // Check if old schema exists and migration is needed
                var oldSchemaExists = await CheckTableExistsAsync("Providers", cancellationToken);
                
                if (oldSchemaExists)
                {
                    _logger.LogInformation("Users table exists. Checking if migration is needed...");
                    await MigrateDataFromOldSchemaAsync(cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database migration.");
            throw;
        }
    }

    private async Task<bool> CheckTableExistsAsync(string tableName, CancellationToken cancellationToken)
    {
        try
        {
            // Use INFORMATION_SCHEMA to check if table exists (more reliable and doesn't throw errors)
            // Use direct database connection to execute scalar query
            var connection = _dbContext.Database.GetDbConnection();
            var wasOpen = connection.State == System.Data.ConnectionState.Open;
            if (!wasOpen)
            {
                await connection.OpenAsync(cancellationToken);
            }
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = $"SELECT CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}') THEN 1 ELSE 0 END";
                var exists = await command.ExecuteScalarAsync(cancellationToken);
                return exists != null && Convert.ToInt32(exists) == 1;
            }
            finally
            {
                if (!wasOpen)
                {
                    await connection.CloseAsync();
                }
            }
        }
        catch (Exception ex)
        {
            // If we can't check, assume table doesn't exist
            _logger.LogDebug("Could not check if table {TableName} exists: {Error}", tableName, ex.Message);
            return false;
        }
    }

    private async Task EnsureUserAttributesTableExistsAsync(CancellationToken cancellationToken)
    {
        var tableExists = await CheckTableExistsAsync("UserAttributes", cancellationToken);
        if (!tableExists)
        {
            _logger.LogInformation("Creating UserAttributes table...");
            try
            {
                await _dbContext.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserAttributes')
                    BEGIN
                        CREATE TABLE UserAttributes (
                            Id INT PRIMARY KEY IDENTITY(1,1),
                            UserId INT NOT NULL,
                            AttributeType INT NOT NULL,
                            CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                            FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
                            CONSTRAINT UQ_UserAttributes_UserId_AttributeType UNIQUE (UserId, AttributeType)
                        );
                        CREATE INDEX IX_UserAttributes_UserId ON UserAttributes(UserId);
                    END
                ", cancellationToken);
                _logger.LogInformation("UserAttributes table created.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create UserAttributes table.");
                throw;
            }
        }
    }

    private async Task EnsureOperatorCoveragesTableExistsAsync(CancellationToken cancellationToken)
    {
        var tableExists = await CheckTableExistsAsync("OperatorCoverages", cancellationToken);
        if (!tableExists)
        {
            _logger.LogInformation("Creating OperatorCoverages table...");
            try
            {
                await _dbContext.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OperatorCoverages')
                    BEGIN
                        CREATE TABLE OperatorCoverages (
                            Id INT PRIMARY KEY IDENTITY(1,1),
                            OperatorId INT NOT NULL,
                            BaseLocationName NVARCHAR(MAX) NOT NULL,
                            Latitude FLOAT NULL,
                            Longitude FLOAT NULL,
                            CoverageRadiusKm FLOAT NOT NULL,
                            MinPassengerCapacity INT NOT NULL,
                            MaxPassengerCapacity INT NOT NULL,
                            CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                            UpdatedAt DATETIME2 NULL,
                            IsGeocoded BIT NOT NULL DEFAULT 0,
                            GeocodingError NVARCHAR(MAX) NULL,
                            FOREIGN KEY (OperatorId) REFERENCES Users(Id) ON DELETE CASCADE
                        );
                        CREATE INDEX IX_OperatorCoverages_OperatorId ON OperatorCoverages(OperatorId);
                    END
                ", cancellationToken);
                _logger.LogInformation("OperatorCoverages table created.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create OperatorCoverages table.");
                throw;
            }
        }
    }

    private async Task EnsureRawJsonPayloadColumnExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Check if CharterRequests table exists
            var tableExists = await CheckTableExistsAsync("CharterRequests", cancellationToken);
            if (!tableExists)
            {
                _logger.LogDebug("CharterRequests table does not exist, skipping RawJsonPayload column check.");
                return;
            }

            // Add RawJsonPayload column if it doesn't exist (using IF NOT EXISTS)
            _logger.LogInformation("Checking for RawJsonPayload column in CharterRequests table...");
            try
            {
                await _dbContext.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (
                        SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = 'CharterRequests' AND COLUMN_NAME = 'RawJsonPayload'
                    )
                    BEGIN
                        ALTER TABLE CharterRequests 
                        ADD RawJsonPayload NVARCHAR(MAX) NULL
                        PRINT 'RawJsonPayload column added successfully.'
                    END
                ", cancellationToken);
                _logger.LogInformation("RawJsonPayload column check completed.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not add RawJsonPayload column (it may already exist): {Error}", ex.Message);
                // Don't throw - this is not critical
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not check/add RawJsonPayload column: {Error}", ex.Message);
            // Don't throw - this is not critical
        }
    }

    private async Task EnsureQuoteDeadlineColumnExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var tableExists = await CheckTableExistsAsync("CharterRequests", cancellationToken);
            if (!tableExists)
            {
                _logger.LogDebug("CharterRequests table does not exist, skipping QuoteDeadline column check.");
                return;
            }

            _logger.LogInformation("Checking for QuoteDeadline column in CharterRequests table...");
            try
            {
                await _dbContext.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (
                        SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = 'CharterRequests' AND COLUMN_NAME = 'QuoteDeadline'
                    )
                    BEGIN
                        ALTER TABLE CharterRequests 
                        ADD QuoteDeadline DATETIME2 NULL
                        PRINT 'QuoteDeadline column added successfully.'
                    END
                ", cancellationToken);
                _logger.LogInformation("QuoteDeadline column check completed.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not add QuoteDeadline column (it may already exist): {Error}", ex.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not check/add QuoteDeadline column: {Error}", ex.Message);
        }
    }

    private async Task EnsureEmailColumnExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var tableExists = await CheckTableExistsAsync("CharterRequests", cancellationToken);
            if (!tableExists)
            {
                _logger.LogDebug("CharterRequests table does not exist, skipping Email column check.");
                return;
            }

            _logger.LogInformation("Checking for Email column in CharterRequests table...");
            try
            {
                await _dbContext.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (
                        SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = 'CharterRequests' AND COLUMN_NAME = 'Email'
                    )
                    BEGIN
                        ALTER TABLE CharterRequests 
                        ADD Email NVARCHAR(MAX) NULL
                        PRINT 'Email column added successfully.'
                    END
                ", cancellationToken);
                _logger.LogInformation("Email column check completed.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not add Email column (it may already exist): {Error}", ex.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not check/add Email column: {Error}", ex.Message);
        }
    }

    private async Task DropOldTypeColumnsAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Drop OperatorType column if it exists (using IF EXISTS to avoid errors)
            _logger.LogInformation("Checking for OperatorType column...");
            try
            {
                await _dbContext.Database.ExecuteSqlRawAsync(@"
                    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'OperatorType')
                    BEGIN
                        ALTER TABLE Users DROP COLUMN OperatorType
                        PRINT 'OperatorType column dropped successfully.'
                    END
                ", cancellationToken);
                _logger.LogInformation("OperatorType column check/drop completed.");
            }
            catch (Exception dropEx)
            {
                _logger.LogWarning(dropEx, "Could not drop OperatorType column (it may not exist): {Error}", dropEx.Message);
            }

            // Drop RequesterType column if it exists (using IF EXISTS to avoid errors)
            _logger.LogInformation("Checking for RequesterType column...");
            try
            {
                await _dbContext.Database.ExecuteSqlRawAsync(@"
                    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'RequesterType')
                    BEGIN
                        ALTER TABLE Users DROP COLUMN RequesterType
                        PRINT 'RequesterType column dropped successfully.'
                    END
                ", cancellationToken);
                _logger.LogInformation("RequesterType column check/drop completed.");
            }
            catch (Exception dropEx)
            {
                _logger.LogWarning(dropEx, "Could not drop RequesterType column (it may not exist): {Error}", dropEx.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error attempting to drop old type columns: {Error}", ex.Message);
            // Don't throw - log the error but continue, as these columns may not exist or may need manual removal
            _logger.LogInformation("If columns still exist, please run Scripts/DropOldTypeColumns.sql manually.");
        }
    }

    private async Task CreateUsersTableAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.Database.EnsureCreatedAsync(cancellationToken);
            await EnsureUserAttributesTableExistsAsync(cancellationToken);
            _logger.LogInformation("Users table verified/created.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Users table. Attempting to create it manually...");
            // Try to create Users table manually if EnsureCreated fails
            try
            {
                await _dbContext.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
                    BEGIN
                        CREATE TABLE Users (
                            Id INT PRIMARY KEY IDENTITY(1,1),
                            Email NVARCHAR(450) NOT NULL UNIQUE,
                            Name NVARCHAR(MAX) NOT NULL,
                            Phone NVARCHAR(MAX) NULL,
                            CompanyName NVARCHAR(MAX) NULL,
                            ExternalId NVARCHAR(MAX) NOT NULL,
                            ExternalProvider NVARCHAR(MAX) NOT NULL,
                            PasswordHash NVARCHAR(MAX) NULL,
                            Role INT NOT NULL DEFAULT 0,
                            CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                            LastLoginAt DATETIME2 NULL,
                            IsActive BIT NOT NULL DEFAULT 1
                        );
                        CREATE UNIQUE INDEX IX_Users_Email ON Users(Email);
                        CREATE UNIQUE INDEX IX_Users_ExternalId_ExternalProvider ON Users(ExternalId, ExternalProvider);
                    END
                    
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserAttributes')
                    BEGIN
                        CREATE TABLE UserAttributes (
                            Id INT PRIMARY KEY IDENTITY(1,1),
                            UserId INT NOT NULL,
                            AttributeType INT NOT NULL,
                            CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                            FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
                            UNIQUE (UserId, AttributeType)
                        );
                        CREATE INDEX IX_UserAttributes_UserId ON UserAttributes(UserId);
                    END
                ", cancellationToken);
                _logger.LogInformation("Users table created manually.");
            }
            catch (Exception manualEx)
            {
                _logger.LogError(manualEx, "Failed to create Users table manually.");
                throw;
            }
        }
    }

    private async Task MigrateDataFromOldSchemaAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Check if migration has already been run
            var userCount = await _dbContext.Database.SqlQueryRaw<int>("SELECT COUNT(*) FROM Users").FirstOrDefaultAsync(cancellationToken);
            var providerCount = await _dbContext.Database.SqlQueryRaw<int>("SELECT COUNT(*) FROM Providers").FirstOrDefaultAsync(cancellationToken);
            var requesterCount = await _dbContext.Database.SqlQueryRaw<int>("SELECT COUNT(*) FROM Requesters").FirstOrDefaultAsync(cancellationToken);
            
            // If Users has data and matches Providers + Requesters, migration likely already ran
            if (userCount > 0 && userCount >= (providerCount + requesterCount))
            {
                _logger.LogInformation("Migration appears to have already been completed. User count: {UserCount}, Provider count: {ProviderCount}, Requester count: {RequesterCount}", 
                    userCount, providerCount, requesterCount);
                return;
            }

            _logger.LogWarning("Migration needed. Users: {UserCount}, Providers: {ProviderCount}, Requesters: {RequesterCount}. Running migration...", 
                userCount, providerCount, requesterCount);
            
            // Run migration in a transaction
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Migrate Providers to Users
                _logger.LogInformation("Migrating Providers to Users...");
                await _dbContext.Database.ExecuteSqlRawAsync(@"
                    INSERT INTO Users (Email, Name, CompanyName, Phone, ExternalId, ExternalProvider, PasswordHash, Role, CreatedAt, LastLoginAt, IsActive)
                    SELECT 
                        Email,
                        Name,
                        CompanyName,
                        Phone,
                        ExternalId,
                        ExternalProvider,
                        PasswordHash,
                        CASE 
                            WHEN IsAdmin = 1 THEN 2  -- UserRole.Admin = 2
                            ELSE 1                    -- UserRole.Operator = 1
                        END AS Role,
                        CreatedAt,
                        LastLoginAt,
                        IsActive
                    FROM Providers
                    WHERE NOT EXISTS (
                        SELECT 1 FROM Users WHERE Users.Email = Providers.Email
                    )
                ", cancellationToken);

                // Migrate Requesters to Users
                _logger.LogInformation("Migrating Requesters to Users...");
                await _dbContext.Database.ExecuteSqlRawAsync(@"
                    INSERT INTO Users (Email, Name, Phone, ExternalId, ExternalProvider, PasswordHash, Role, CreatedAt, LastLoginAt, IsActive)
                    SELECT 
                        Email,
                        Name,
                        Phone,
                        ExternalId,
                        ExternalProvider,
                        PasswordHash,
                        0 AS Role,  -- UserRole.Requester = 0
                        CreatedAt,
                        LastLoginAt,
                        IsActive
                    FROM Requesters
                    WHERE NOT EXISTS (
                        SELECT 1 FROM Users WHERE Users.Email = Requesters.Email
                    )
                ", cancellationToken);

                // Add default attributes for migrated users
                _logger.LogInformation("Adding default attributes for migrated users...");
                
                // Add Bus attribute for all operators/admins
                await _dbContext.Database.ExecuteSqlRawAsync(@"
                    INSERT INTO UserAttributes (UserId, AttributeType, CreatedAt)
                    SELECT Id, 1, GETUTCDATE()  -- 1 = Bus
                    FROM Users
                    WHERE Role IN (1, 2)  -- Operator or Admin
                    AND NOT EXISTS (
                        SELECT 1 FROM UserAttributes WHERE UserId = Users.Id AND AttributeType = 1
                    )
                ", cancellationToken);

                // Add Individual attribute for all requesters
                await _dbContext.Database.ExecuteSqlRawAsync(@"
                    INSERT INTO UserAttributes (UserId, AttributeType, CreatedAt)
                    SELECT Id, 10, GETUTCDATE()  -- 10 = Individual
                    FROM Users
                    WHERE Role = 0  -- Requester
                    AND NOT EXISTS (
                        SELECT 1 FROM UserAttributes WHERE UserId = Users.Id AND AttributeType = 10
                    )
                ", cancellationToken);

                // Update foreign keys in Quotes table
                _logger.LogInformation("Updating foreign keys in Quotes table...");
                await _dbContext.Database.ExecuteSqlRawAsync(@"
                    UPDATE Quotes
                    SET ProviderId = (
                        SELECT u.Id 
                        FROM Users u
                        INNER JOIN Providers p ON p.Email = u.Email
                        WHERE p.Id = Quotes.ProviderId
                    )
                    WHERE EXISTS (
                        SELECT 1 
                        FROM Providers p
                        INNER JOIN Users u ON u.Email = p.Email
                        WHERE p.Id = Quotes.ProviderId
                    )
                    AND ProviderId NOT IN (SELECT Id FROM Users)
                ", cancellationToken);

                // Update foreign keys in CharterRequests table
                _logger.LogInformation("Updating foreign keys in CharterRequests table...");
                await _dbContext.Database.ExecuteSqlRawAsync(@"
                    UPDATE CharterRequests
                    SET RequesterId = (
                        SELECT u.Id 
                        FROM Users u
                        INNER JOIN Requesters r ON r.Email = u.Email
                        WHERE r.Id = CharterRequests.RequesterId
                    )
                    WHERE RequesterId IS NOT NULL
                    AND EXISTS (
                        SELECT 1 
                        FROM Requesters r
                        INNER JOIN Users u ON u.Email = r.Email
                        WHERE r.Id = CharterRequests.RequesterId
                    )
                    AND RequesterId NOT IN (SELECT Id FROM Users)
                ", cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("Migration completed successfully!");
            }
            catch (Exception migrationEx)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(migrationEx, "Migration failed. Rolling back transaction.");
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during automatic migration.");
            throw;
        }
    }
}

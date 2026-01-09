-- Migration Script: Migrate from Providers/Requesters to unified Users table
-- Run this script AFTER the new schema is created (EnsureCreated will create Users table)
-- This script migrates data from the old tables to the new unified Users table

BEGIN TRANSACTION;

-- Step 1: Create UserAttributes table if it doesn't exist
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

-- Step 2: Migrate Providers to Users
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
);

-- Step 3: Migrate Requesters to Users
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
);

-- Step 4: Add default attributes for migrated users
-- Add Bus attribute (1) for all operators/admins
INSERT INTO UserAttributes (UserId, AttributeType, CreatedAt)
SELECT Id, 1, GETUTCDATE()  -- 1 = Bus
FROM Users
WHERE Role IN (1, 2)  -- Operator or Admin
AND NOT EXISTS (
    SELECT 1 FROM UserAttributes WHERE UserId = Users.Id AND AttributeType = 1
);

-- Add Individual attribute (10) for all requesters
INSERT INTO UserAttributes (UserId, AttributeType, CreatedAt)
SELECT Id, 10, GETUTCDATE()  -- 10 = Individual
FROM Users
WHERE Role = 0  -- Requester
AND NOT EXISTS (
    SELECT 1 FROM UserAttributes WHERE UserId = Users.Id AND AttributeType = 10
);

-- Step 5: Update foreign keys in Quotes table
-- First, create a mapping table to track old ProviderId -> new UserId
-- Then update Quotes.ProviderId to point to Users.Id

-- Update Quotes to use new User IDs (matching by Email since Email is unique)
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
);

-- Step 6: Update foreign keys in CharterRequests table
-- Update CharterRequests.RequesterId to point to Users.Id

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
);

-- Note: After migration is complete and verified, you can drop the old tables:
-- DROP TABLE Providers;
-- DROP TABLE Requesters;

COMMIT TRANSACTION;

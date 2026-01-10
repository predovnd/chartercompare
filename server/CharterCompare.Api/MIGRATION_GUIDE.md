# Migration Guide: Unified User Model

## Overview

The application has been refactored to use a unified `Users` table instead of separate `Providers` and `Requesters` tables. This eliminates code duplication and simplifies user management.

## What Changed

### Database Schema
- **New Table**: `Users` - Unified table for all user types
- **Removed Tables**: `Providers`, `Requesters` (after migration)
- **UserRole Enum**: 
  - `Requester = 0`
  - `Operator = 1`
  - `Admin = 2`

### Code Changes
- All handlers now use `User` entity instead of `Operator`/`Requester`
- `IStorage` interface updated with unified user methods
- Authentication still supports legacy claims (`ProviderId`, `RequesterId`) for backward compatibility
- New `UserId` claim added for future use

## Migration Steps

### For New Databases
No migration needed - the new schema will be created automatically when you run the application.

### For Existing Databases

1. **Backup your database** before running migration

2. **Run the application once** to create the new `Users` table:
   ```bash
   dotnet run
   ```
   The application will detect the old schema and create the new `Users` table.

3. **Run the migration script** in SQL Server Management Studio:
   - Open `Scripts/MigrateToUnifiedUsers.sql`
   - Execute it against your database
   - This will:
     - Migrate all Providers to Users (with Role = Operator or Admin)
     - Migrate all Requesters to Users (with Role = Requester)
     - Update foreign keys in Quotes and CharterRequests tables

4. **Verify the migration**:
   ```sql
   -- Check user counts
   SELECT Role, COUNT(*) FROM Users GROUP BY Role;
   
   -- Verify no data loss
   SELECT COUNT(*) FROM Users; -- Should match Providers + Requesters count
   ```

5. **After verification, drop old tables** (optional):
   ```sql
   DROP TABLE Providers;
   DROP TABLE Requesters;
   ```

## Backward Compatibility

The application maintains backward compatibility:
- Legacy claims (`ProviderId`, `RequesterId`) are still set during authentication
- New `UserId` claim is also set for future use
- Handlers check for both new and legacy claims

## Testing

After migration, test:
1. Operator login (email/password and Google OAuth)
2. Requester login (email/password and Google OAuth)
3. Admin login
4. Creating quotes as operator
5. Viewing requests as requester
6. Admin dashboard user list

## Rollback

If you need to rollback:
1. Restore database from backup
2. Revert code changes to previous commit
3. Restart application

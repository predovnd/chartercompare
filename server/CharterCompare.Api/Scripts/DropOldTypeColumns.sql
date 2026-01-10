-- Script to drop old OperatorType and RequesterType columns from Users table
-- These columns are no longer used after migration to UserAttributes system

BEGIN TRANSACTION;

-- Drop OperatorType column if it exists
IF EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'OperatorType'
)
BEGIN
    ALTER TABLE Users DROP COLUMN OperatorType;
    PRINT 'OperatorType column dropped successfully.';
END
ELSE
BEGIN
    PRINT 'OperatorType column does not exist.';
END

-- Drop RequesterType column if it exists
IF EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'RequesterType'
)
BEGIN
    ALTER TABLE Users DROP COLUMN RequesterType;
    PRINT 'RequesterType column dropped successfully.';
END
ELSE
BEGIN
    PRINT 'RequesterType column does not exist.';
END

COMMIT TRANSACTION;

PRINT 'Script completed.';

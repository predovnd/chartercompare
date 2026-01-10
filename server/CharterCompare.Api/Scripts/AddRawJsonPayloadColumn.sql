-- Script to add RawJsonPayload column to CharterRequests table
-- This column stores the raw JSON payload for admin viewing

IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'CharterRequests' AND COLUMN_NAME = 'RawJsonPayload'
)
BEGIN
    ALTER TABLE CharterRequests 
    ADD RawJsonPayload NVARCHAR(MAX) NULL;
    
    PRINT 'RawJsonPayload column added successfully.';
END
ELSE
BEGIN
    PRINT 'RawJsonPayload column already exists.';
END

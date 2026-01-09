-- Script to create OperatorCoverages table
-- This table stores operator coverage configuration (base location, radius, capacity)

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
    
    PRINT 'OperatorCoverages table created successfully.';
END
ELSE
BEGIN
    PRINT 'OperatorCoverages table already exists.';
END

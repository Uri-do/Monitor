-- Add KPI Execution Tracking Migration - Part 3
-- This script creates remaining tables, constraints, and seeds initial data
USE [PopAI]
GO

PRINT '=== Starting KPI Execution Tracking Migration - Part 3 ==='
PRINT 'Creating remaining tables, constraints, and seeding data...'
PRINT ''

-- Create RefreshTokens table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'auth.RefreshTokens') AND type in (N'U'))
BEGIN
    CREATE TABLE auth.RefreshTokens (
        Id INT IDENTITY(1,1) NOT NULL,
        UserId NVARCHAR(50) NOT NULL,
        Token NVARCHAR(255) NOT NULL,
        ExpiresAt DATETIME2 NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        IsActive BIT NOT NULL,
        RevokedAt DATETIME2 NULL,
        RevokedBy NVARCHAR(100) NULL,
        RevokedReason NVARCHAR(500) NULL,
        IpAddress NVARCHAR(45) NULL,
        UserAgent NVARCHAR(500) NULL,
        CONSTRAINT PK_RefreshTokens PRIMARY KEY (Id),
        CONSTRAINT FK_RefreshTokens_Users_UserId FOREIGN KEY (UserId) REFERENCES auth.Users(UserId) ON DELETE CASCADE
    );
    
    -- Create indexes
    CREATE INDEX IX_RefreshTokens_Active_Expires ON auth.RefreshTokens (IsActive, ExpiresAt);
    CREATE UNIQUE INDEX IX_RefreshTokens_Token ON auth.RefreshTokens (Token);
    CREATE INDEX IX_RefreshTokens_UserId ON auth.RefreshTokens (UserId);
    
    PRINT '✅ Created RefreshTokens table with indexes and foreign key'
END
ELSE
BEGIN
    PRINT '⚠️ RefreshTokens table already exists'
END

-- Create UserPasswords table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'auth.UserPasswords') AND type in (N'U'))
BEGIN
    CREATE TABLE auth.UserPasswords (
        Id INT IDENTITY(1,1) NOT NULL,
        UserId NVARCHAR(50) NOT NULL,
        PasswordHash NVARCHAR(255) NOT NULL,
        PasswordSalt NVARCHAR(255) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        IsActive BIT NOT NULL,
        CreatedBy NVARCHAR(100) NULL,
        CONSTRAINT PK_UserPasswords PRIMARY KEY (Id),
        CONSTRAINT FK_UserPasswords_Users_UserId FOREIGN KEY (UserId) REFERENCES auth.Users(UserId) ON DELETE CASCADE
    );
    
    -- Create indexes
    CREATE INDEX IX_UserPasswords_UserId ON auth.UserPasswords (UserId);
    CREATE INDEX IX_UserPasswords_UserId_Active ON auth.UserPasswords (UserId, IsActive);
    
    PRINT '✅ Created UserPasswords table with indexes and foreign key'
END
ELSE
BEGIN
    PRINT '⚠️ UserPasswords table already exists'
END

-- Create SecurityAuditEvents table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.SecurityAuditEvents') AND type in (N'U'))
BEGIN
    CREATE TABLE dbo.SecurityAuditEvents (
        EventId NVARCHAR(50) NOT NULL,
        EventType NVARCHAR(100) NOT NULL,
        UserId NVARCHAR(50) NULL,
        Username NVARCHAR(100) NULL,
        IpAddress NVARCHAR(45) NULL,
        UserAgent NVARCHAR(500) NULL,
        Resource NVARCHAR(200) NULL,
        Action NVARCHAR(100) NULL,
        IsSuccess BIT NOT NULL,
        ErrorMessage NVARCHAR(1000) NULL,
        AdditionalData NVARCHAR(MAX) NOT NULL,
        Timestamp DATETIME2 NOT NULL,
        Severity NVARCHAR(50) NOT NULL,
        CONSTRAINT PK_SecurityAuditEvents PRIMARY KEY (EventId)
    );
    
    -- Create indexes
    CREATE INDEX IX_SecurityAuditEvents_EventType ON dbo.SecurityAuditEvents (EventType);
    CREATE INDEX IX_SecurityAuditEvents_EventType_Timestamp ON dbo.SecurityAuditEvents (EventType, Timestamp);
    CREATE INDEX IX_SecurityAuditEvents_Timestamp ON dbo.SecurityAuditEvents (Timestamp);
    CREATE INDEX IX_SecurityAuditEvents_UserId ON dbo.SecurityAuditEvents (UserId);
    CREATE INDEX IX_SecurityAuditEvents_UserId_Timestamp ON dbo.SecurityAuditEvents (UserId, Timestamp);
    
    PRINT '✅ Created SecurityAuditEvents table with indexes'
END
ELSE
BEGIN
    PRINT '⚠️ SecurityAuditEvents table already exists'
END

-- Add check constraints to KPIs table
PRINT ''
PRINT 'Adding check constraints to KPIs table...'

-- ComparisonOperator constraint
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_KPIs_ComparisonOperator')
BEGIN
    ALTER TABLE monitoring.KPIs ADD CONSTRAINT CK_KPIs_ComparisonOperator 
        CHECK (ComparisonOperator IS NULL OR ComparisonOperator IN ('gt', 'gte', 'lt', 'lte', 'eq'));
    PRINT '✅ Added ComparisonOperator check constraint'
END
ELSE
BEGIN
    PRINT '⚠️ ComparisonOperator check constraint already exists'
END

-- KpiType constraint
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_KPIs_KpiType')
BEGIN
    ALTER TABLE monitoring.KPIs ADD CONSTRAINT CK_KPIs_KpiType 
        CHECK (KpiType IN ('success_rate', 'transaction_volume', 'threshold', 'trend_analysis'));
    PRINT '✅ Added KpiType check constraint'
END
ELSE
BEGIN
    PRINT '⚠️ KpiType check constraint already exists'
END

-- Add foreign key constraint from KPIs to KpiTypes
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_KPIs_KpiTypes_KpiType')
BEGIN
    ALTER TABLE monitoring.KPIs ADD CONSTRAINT FK_KPIs_KpiTypes_KpiType 
        FOREIGN KEY (KpiType) REFERENCES monitoring.KpiTypes(KpiTypeId);
    PRINT '✅ Added foreign key constraint from KPIs to KpiTypes'
END
ELSE
BEGIN
    PRINT '⚠️ Foreign key constraint from KPIs to KpiTypes already exists'
END

-- Add index on KPIs.KpiType
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_KPIs_KpiType')
BEGIN
    CREATE INDEX IX_KPIs_KpiType ON monitoring.KPIs (KpiType) INCLUDE (IsActive);
    PRINT '✅ Added index on KPIs.KpiType'
END
ELSE
BEGIN
    PRINT '⚠️ Index on KPIs.KpiType already exists'
END

-- Seed KpiTypes data
PRINT ''
PRINT 'Seeding KpiTypes data...'

MERGE monitoring.KpiTypes AS target
USING (VALUES 
    ('success_rate', 'Success Rate Monitoring', 'Monitors success percentages and compares them against historical averages. Ideal for tracking transaction success rates, API response rates, login success rates, and other percentage-based metrics.', '["deviation", "lastMinutes"]', 'monitoring.usp_MonitorTransactions'),
    ('transaction_volume', 'Transaction Volume Monitoring', 'Tracks transaction counts and compares them to historical patterns. Perfect for detecting unusual spikes or drops in activity, monitoring daily transactions, API calls, user registrations, and other count-based metrics.', '["deviation", "minimumThreshold", "lastMinutes"]', 'monitoring.usp_MonitorTransactionVolume'),
    ('threshold', 'Threshold Monitoring', 'Simple threshold-based monitoring that triggers alerts when values cross specified limits. Useful for monitoring system resources, queue lengths, error counts, response times, and other absolute value metrics.', '["thresholdValue", "comparisonOperator"]', 'monitoring.usp_MonitorThreshold'),
    ('trend_analysis', 'Trend Analysis', 'Analyzes trends over time to detect gradual changes or patterns. Excellent for capacity planning, performance degradation detection, user behavior analysis, and early warning systems for emerging issues.', '["deviation", "lastMinutes"]', 'monitoring.usp_MonitorTrends')
) AS source (KpiTypeId, Name, Description, RequiredFields, DefaultStoredProcedure)
ON target.KpiTypeId = source.KpiTypeId
WHEN MATCHED THEN
    UPDATE SET 
        Name = source.Name,
        Description = source.Description,
        RequiredFields = source.RequiredFields,
        DefaultStoredProcedure = source.DefaultStoredProcedure,
        ModifiedDate = GETUTCDATE()
WHEN NOT MATCHED THEN
    INSERT (KpiTypeId, Name, Description, RequiredFields, DefaultStoredProcedure, IsActive, CreatedDate, ModifiedDate)
    VALUES (source.KpiTypeId, source.Name, source.Description, source.RequiredFields, source.DefaultStoredProcedure, 1, GETUTCDATE(), GETUTCDATE());

PRINT '✅ Seeded KpiTypes data'

-- Update existing KPIs to have proper KpiType values
PRINT ''
PRINT 'Updating existing KPIs with proper KpiType values...'

UPDATE monitoring.KPIs 
SET KpiType = 'success_rate', ModifiedDate = GETUTCDATE()
WHERE KpiType = 'success_rate' OR SpName LIKE '%MonitorTransactions%' OR Indicator LIKE '%Success Rate%';

UPDATE monitoring.KPIs 
SET KpiType = 'threshold', ModifiedDate = GETUTCDATE()
WHERE ThresholdValue IS NOT NULL AND ComparisonOperator IS NOT NULL;

PRINT '✅ Updated existing KPIs with proper KpiType values'

PRINT ''
PRINT '=== Migration Complete! ==='
PRINT 'All tables, columns, constraints, and initial data have been added successfully.'
PRINT ''
PRINT 'Summary of changes:'
PRINT '- Added execution tracking columns to KPIs table'
PRINT '- Added comprehensive audit columns to HistoricalData table'
PRINT '- Added enhanced columns to Config table'
PRINT '- Created complete authentication system (Users, Roles, Permissions, etc.)'
PRINT '- Created KpiTypes and ScheduledJobs tables'
PRINT '- Created SecurityAuditEvents table'
PRINT '- Added all necessary indexes and constraints'
PRINT '- Seeded initial KPI type data'
PRINT ''
PRINT 'Your MonitoringGrid system is now ready for enhanced real-time monitoring!'
GO

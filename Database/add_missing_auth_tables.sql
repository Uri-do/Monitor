-- Add Missing Authentication Tables for MonitoringGrid
-- Run this script on the PopAI database to add SecurityThreats and UserTwoFactorSettings tables
USE [PopAI]
GO

PRINT '=== Adding Missing Authentication Tables ==='

-- Check and create SecurityThreats table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'auth.SecurityThreats') AND type in (N'U'))
BEGIN
    PRINT 'Creating auth.SecurityThreats table...'

    CREATE TABLE [auth].[SecurityThreats] (
        [ThreatId] nvarchar(50) NOT NULL,
        [ThreatType] nvarchar(100) NOT NULL,
        [Severity] nvarchar(50) NOT NULL,
        [Description] nvarchar(1000) NOT NULL,
        [UserId] nvarchar(50) NULL,
        [IpAddress] nvarchar(45) NULL,
        [DetectedAt] datetime2 NOT NULL,
        [IsResolved] bit NOT NULL,
        [ResolvedAt] datetime2 NULL,
        [Resolution] nvarchar(1000) NULL,
        [ThreatData] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_SecurityThreats] PRIMARY KEY ([ThreatId])
    );

    -- Create indexes for SecurityThreats
    CREATE INDEX [IX_SecurityThreats_ThreatType] ON [auth].[SecurityThreats] ([ThreatType]);
    CREATE INDEX [IX_SecurityThreats_Severity] ON [auth].[SecurityThreats] ([Severity]);
    CREATE INDEX [IX_SecurityThreats_UserId] ON [auth].[SecurityThreats] ([UserId]);
    CREATE INDEX [IX_SecurityThreats_IpAddress] ON [auth].[SecurityThreats] ([IpAddress]);
    CREATE INDEX [IX_SecurityThreats_DetectedAt] ON [auth].[SecurityThreats] ([DetectedAt]);
    CREATE INDEX [IX_SecurityThreats_IsResolved] ON [auth].[SecurityThreats] ([IsResolved]);

    PRINT '✅ auth.SecurityThreats table created successfully'
END
ELSE
BEGIN
    PRINT '⚠️ auth.SecurityThreats table already exists'
END

-- Check and create UserTwoFactorSettings table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'auth.UserTwoFactorSettings') AND type in (N'U'))
BEGIN
    PRINT 'Creating auth.UserTwoFactorSettings table...'

    CREATE TABLE [auth].[UserTwoFactorSettings] (
        [UserId] nvarchar(50) NOT NULL,
        [IsEnabled] bit NOT NULL,
        [Secret] nvarchar(255) NULL,
        [RecoveryCodes] nvarchar(max) NOT NULL,
        [EnabledAt] datetime2 NULL,
        CONSTRAINT [PK_UserTwoFactorSettings] PRIMARY KEY ([UserId])
    );

    -- Create indexes for UserTwoFactorSettings
    CREATE INDEX [IX_UserTwoFactorSettings_IsEnabled] ON [auth].[UserTwoFactorSettings] ([IsEnabled]);
    CREATE INDEX [IX_UserTwoFactorSettings_EnabledAt] ON [auth].[UserTwoFactorSettings] ([EnabledAt]);

    PRINT '✅ auth.UserTwoFactorSettings table created successfully'
END
ELSE
BEGIN
    PRINT '⚠️ auth.UserTwoFactorSettings table already exists'
END

-- Update migration history to mark this migration as applied (if __EFMigrationsHistory table exists)
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'__EFMigrationsHistory') AND type in (N'U'))
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20250610001114_AddSecurityThreatAndUserTwoFactorSettings')
    BEGIN
        INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
        VALUES ('20250610001114_AddSecurityThreatAndUserTwoFactorSettings', '8.0.0')

        PRINT '✅ Migration history updated'
    END
    ELSE
    BEGIN
        PRINT '⚠️ Migration already recorded in history'
    END
END
ELSE
BEGIN
    PRINT '⚠️ __EFMigrationsHistory table not found - skipping migration history update'
END

-- Verify tables were created
PRINT ''
PRINT '=== Verification ==='

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'auth.SecurityThreats') AND type in (N'U'))
    PRINT '✅ auth.SecurityThreats table exists'
ELSE
    PRINT '❌ auth.SecurityThreats table missing'

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'auth.UserTwoFactorSettings') AND type in (N'U'))
    PRINT '✅ auth.UserTwoFactorSettings table exists'
ELSE
    PRINT '❌ auth.UserTwoFactorSettings table missing'

PRINT ''
PRINT '=== Script Complete ==='
PRINT 'You can now restart the MonitoringGrid API to test authentication'

-- Create PopAI Database for Monitoring Grid System
-- This script creates the PopAI database if it doesn't exist

-- Connect to master database to create the new database
USE master
GO

-- Check if PopAI database exists, create if not
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'PopAI')
BEGIN
    CREATE DATABASE [PopAI]
    ON 
    ( NAME = 'PopAI_Data',
      FILENAME = 'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\PopAI.mdf',
      SIZE = 100MB,
      MAXSIZE = 1GB,
      FILEGROWTH = 10MB )
    LOG ON 
    ( NAME = 'PopAI_Log',
      FILENAME = 'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\PopAI.ldf',
      SIZE = 10MB,
      MAXSIZE = 100MB,
      FILEGROWTH = 1MB )
    
    PRINT 'PopAI database created successfully'
END
ELSE
BEGIN
    PRINT 'PopAI database already exists'
END
GO

-- Set database options for optimal performance
ALTER DATABASE [PopAI] SET RECOVERY SIMPLE
GO

ALTER DATABASE [PopAI] SET AUTO_CLOSE OFF
GO

ALTER DATABASE [PopAI] SET AUTO_SHRINK OFF
GO

ALTER DATABASE [PopAI] SET AUTO_CREATE_STATISTICS ON
GO

ALTER DATABASE [PopAI] SET AUTO_UPDATE_STATISTICS ON
GO

-- Grant permissions to the saturn user
USE [PopAI]
GO

-- Create user if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'saturn')
BEGIN
    CREATE USER [saturn] FOR LOGIN [saturn]
    PRINT 'User saturn created in PopAI database'
END
ELSE
BEGIN
    PRINT 'User saturn already exists in PopAI database'
END
GO

-- Grant necessary permissions
ALTER ROLE [db_datareader] ADD MEMBER [saturn]
ALTER ROLE [db_datawriter] ADD MEMBER [saturn]
ALTER ROLE [db_ddladmin] ADD MEMBER [saturn]
GO

-- Grant execute permissions for stored procedures
GRANT EXECUTE TO [saturn]
GO

PRINT 'PopAI database setup completed successfully!'
PRINT 'Next steps:'
PRINT '1. Run 01_CreateSchema.sql to create the monitoring schema'
PRINT '2. Run 02_InitialData.sql to insert initial configuration'
PRINT '3. Run 03_StoredProcedures.sql to create monitoring procedures'

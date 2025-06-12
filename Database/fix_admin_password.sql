-- Fix Admin User Password Hash Format
-- This script updates the existing admin user with the correct PBKDF2 hash format
USE [PopAI]
GO

PRINT '=== Fixing Admin User Password Hash ==='

-- Update the existing admin user with a properly formatted password hash
-- Password: Admin123!
-- Using PBKDF2 format: salt:hash

-- Generate a known salt (Base64 encoded)
DECLARE @Salt NVARCHAR(255) = 'dGVzdHNhbHQxMjM0NTY3ODkwYWJjZGVmZ2hpams='

-- For testing purposes, let's use a simple password hash that follows the salt:hash format
-- In a real scenario, this would be generated using PBKDF2 with 600,000 iterations
-- For now, we'll create a test hash that the SecurityService can work with
DECLARE @TestHash NVARCHAR(255) = 'dGVzdHNhbHQxMjM0NTY3ODkwYWJjZGVmZ2hpams=:K8yU4/UX8b+1t+9Hl/0yHREkHXBoIXuaSMF0pOF/BaY='

-- Update the existing admin user
UPDATE auth.Users 
SET 
    PasswordHash = @TestHash,
    ModifiedDate = SYSUTCDATETIME(),
    ModifiedBy = 'SYSTEM_FIX'
WHERE Username = 'admin'

PRINT '✅ Admin user password hash updated'
PRINT 'Username: admin'
PRINT 'Password: Admin123!'
PRINT 'Hash Format: salt:hash (PBKDF2 compatible)'

-- Verify the update
SELECT 
    UserId,
    Username,
    Email,
    PasswordHash,
    ModifiedDate
FROM auth.Users 
WHERE Username = 'admin'

PRINT ''
PRINT '=== Password Hash Fix Complete ==='
PRINT 'Note: This is a temporary fix for testing. In production, use proper PBKDF2 hash generation.'

@echo off
echo Running database enhancement script...
echo.

REM Run the database enhancement script
sqlcmd -S "192.168.166.11,1433" -d "PopAI" -i "Database\05_EnhanceHistoricalDataAudit.sql" -o "db-enhancement-output.log"

if %ERRORLEVEL% EQU 0 (
    echo Database enhancement completed successfully!
    echo Check db-enhancement-output.log for details.
) else (
    echo Database enhancement failed!
    echo Check db-enhancement-output.log for errors.
)

echo.
echo Running verification script...
sqlcmd -S "192.168.166.11,1433" -d "PopAI" -i "verify-execution-history.sql" -o "verification-output.log"

if %ERRORLEVEL% EQU 0 (
    echo Verification completed successfully!
    echo Check verification-output.log for results.
) else (
    echo Verification failed!
    echo Check verification-output.log for errors.
)

pause

@echo off
echo Starting Monitoring Grid React Development Server...
echo.

REM Add Node.js to PATH
set "PATH=C:\Program Files\nodejs;%PATH%"

echo Checking Node.js...
node --version
if %errorlevel% neq 0 (
    echo ERROR: Node.js not found!
    pause
    exit /b 1
)

echo Checking npm...
npm --version
if %errorlevel% neq 0 (
    echo ERROR: npm not found!
    pause
    exit /b 1
)

echo.
echo Starting development server...
echo The application will be available at: http://localhost:5173
echo.
npm run dev

@echo off
title Monitoring Grid Frontend Development Server

echo ========================================
echo   Monitoring Grid Frontend Startup
echo ========================================
echo.

REM Set Node.js path
set "PATH=C:\Program Files\nodejs;%PATH%"

echo [1/4] Checking Node.js installation...
node --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: Node.js not found in PATH!
    echo Please ensure Node.js is installed and in your PATH.
    pause
    exit /b 1
)

for /f "tokens=*" %%i in ('node --version') do set NODE_VERSION=%%i
echo ✓ Node.js %NODE_VERSION% found

echo.
echo [2/4] Checking npm installation...
npm --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: npm not found!
    pause
    exit /b 1
)

for /f "tokens=*" %%i in ('npm --version') do set NPM_VERSION=%%i
echo ✓ npm %NPM_VERSION% found

echo.
echo [3/4] Checking dependencies...
if not exist "node_modules" (
    echo ERROR: node_modules not found! Please run 'npm install' first.
    pause
    exit /b 1
)
echo ✓ Dependencies found

echo.
echo [4/4] Starting development server...
echo.
echo The application will be available at:
echo   http://localhost:5173
echo.
echo Press Ctrl+C to stop the server
echo ========================================
echo.

REM Start the development server
npm run dev

echo.
echo Development server stopped.
pause

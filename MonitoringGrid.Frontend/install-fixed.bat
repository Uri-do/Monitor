@echo off
echo Setting up Node.js environment...

REM Add Node.js to PATH for this session
set "PATH=C:\Program Files\nodejs;%PATH%"

echo Verifying Node.js installation...
node --version
if %errorlevel% neq 0 (
    echo ERROR: Node.js not found!
    pause
    exit /b 1
)

echo Verifying npm installation...
npm --version
if %errorlevel% neq 0 (
    echo ERROR: npm not found!
    pause
    exit /b 1
)

echo.
echo Installing dependencies...
npm install

if %errorlevel% neq 0 (
    echo ERROR: Failed to install dependencies!
    pause
    exit /b 1
)

echo.
echo SUCCESS: Dependencies installed successfully!
echo.
echo To start the development server, run:
echo   npm run dev
echo.
pause

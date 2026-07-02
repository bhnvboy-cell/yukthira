@echo off
cd /d "%~dp0"
echo ===========================================
echo  YUKTIRA ERP SUITE - One-Click Start
echo ===========================================
echo.

where dotnet >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: .NET SDK not found.
    pause
    exit /b 1
)

echo [1/3] Building...
dotnet build YuktiraERP.sln -c Debug -q
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Build failed!
    pause
    exit /b 1
)
echo   OK.

echo [2/3] Starting API on port 5000...
start "Yuktira-API" dotnet run --project src\YuktiraERP.Api -c Debug
timeout /t 6 >nul

echo [3/3] Starting Web UI on port 5001...
start "Yuktira-Web" dotnet run --project src\YuktiraERP.Web -c Debug

echo.
echo ===========================================
echo  YUKTIRA ERP SUITE IS RUNNING
echo ===========================================
echo  Web UI:     http://localhost:5001
echo  API:        http://localhost:5000
echo  Swagger:    http://localhost:5000/swagger
echo.
echo  Login with password: yuktira123
echo    superadmin  admin  manager  user  readonly
echo.
echo  Close the 2 windows that opened to stop.
echo ===========================================
pause

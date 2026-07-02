@echo off
cd /d "%~dp0\YuktiraERP"
title Yuktira ERP Server

REM === CHECK .NET SDK ===
where dotnet >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: .NET SDK not found. Install from https://dotnet.microsoft.com/download/dotnet/10
    pause
    exit /b 1
)

REM === DETECT LOCAL IP ===
for /f "tokens=2 delims=:" %%a in ('ipconfig ^| findstr /C:"IPv4 Address" /C:"IP Address"') do (
    set "IP=%%a"
    goto :foundip
)
:foundip
set "IP=%IP: =%"
if "%IP%"=="" set "IP=<your-server-ip>"

echo ============================================
echo   YUKTIRA ERP - SERVER MODE
echo ============================================
echo.
echo  Building solution...
dotnet build YuktiraERP.sln -c Debug -q
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Build failed!
    pause
    exit /b 1
)
echo  Build OK.
echo.

REM === OPEN FIREWALL PORTS ===
echo  Opening firewall ports 5000 and 5001...
netsh advfirewall firewall add rule name="YuktiraERP API" dir=in action=allow protocol=TCP localport=5000 >nul 2>nul
netsh advfirewall firewall add rule name="YuktiraERP Web" dir=in action=allow protocol=TCP localport=5001 >nul 2>nul
echo  Firewall rules added (or already exist).
echo.

REM === START SERVICES ===
echo  Starting API on http://0.0.0.0:5000 ...
start "Yuktira-API" dotnet run --project src\YuktiraERP.Api -c Debug --no-build
timeout /t 6 >nul

echo  Starting Web UI on http://0.0.0.0:5001 ...
start "Yuktira-Web" dotnet run --project src\YuktiraERP.Web -c Debug --no-build

echo.
echo ============================================
echo   SERVER IS RUNNING
echo ============================================
echo.
echo  ACCESS FROM THIS SERVER:
echo    Web UI:     http://localhost:5001
echo    API:        http://localhost:5000
echo    Swagger:    http://localhost:5000/swagger
echo.
echo  ACCESS FROM OTHER COMPUTERS:
echo    Web UI:     http://%IP%:5001
echo    API:        http://%IP%:5000
echo    Swagger:    http://%IP%:5000/swagger
echo.
echo  Default login: jdoe / any password  (Client: 1000)
echo.
echo  Close the 2 windows that opened to stop.
echo ============================================
pause

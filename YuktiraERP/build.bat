@echo off
cd /d "%~dp0"
echo ===========================================
echo  YUKTIRA ERP SUITE - Build
echo ===========================================
echo.
echo Restoring packages...
dotnet restore YuktiraERP.sln
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Package restore failed!
    pause
    exit /b 1
)
echo.
echo Building solution...
dotnet build YuktiraERP.sln -c Debug --no-restore
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Build failed!
    pause
    exit /b 1
)
echo.
echo Build successful!
pause

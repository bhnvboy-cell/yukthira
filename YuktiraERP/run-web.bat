@echo off
cd /d "%~dp0"
echo ===========================================
echo  YUKTIRA ERP SUITE - Run Web UI
echo ===========================================
echo.
echo Starting Web UI on http://localhost:5001 ...
echo.
dotnet run --project src\YuktiraERP.Web
pause

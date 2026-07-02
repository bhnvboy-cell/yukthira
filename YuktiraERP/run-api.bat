@echo off
cd /d "%~dp0"
echo ===========================================
echo  YUKTIRA ERP SUITE - Run API Server
echo ===========================================
echo.
echo Starting API on http://localhost:5000 ...
echo Swagger UI: http://localhost:5000/swagger
echo.
dotnet run --project src\YuktiraERP.Api
pause

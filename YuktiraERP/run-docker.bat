@echo off
cd /d "%~dp0"
echo ===========================================
echo  YUKTIRA ERP SUITE - Docker Run
echo ===========================================
echo.
echo Starting all services with Docker Compose...
echo  - PostgreSQL
echo  - API Server
echo  - Web UI
echo  - Apache Reverse Proxy
echo  - Redis Cache
echo.
docker-compose -f scripts\docker-compose.yml -p yuktira up -d --build
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Docker Compose failed! Is Docker running?
    pause
    exit /b 1
)
echo.
echo ===========================================
echo  All services started!
echo ===========================================
echo  Web UI:     http://localhost:5001
echo  API:        http://localhost:5000/swagger
echo  PostgreSQL: localhost:5432
echo  Redis:      localhost:6379
echo.
echo  To stop: docker-compose -f scripts\docker-compose.yml -p yuktira down
echo.
pause

@echo off
cd /d "%~dp0"
echo ===========================================
echo  YUKTIRA ERP SUITE - Initialize Database
echo ===========================================
echo.
echo NOTE: The app uses in-memory services by default,
echo so a database is NOT required to try it out.
echo.
echo This script is only needed if you want to connect
echo to a real PostgreSQL database.
echo.

set PGHOST=localhost
set PGPORT=5432
set PGDATABASE=yuktira_erp
set PGUSER=postgres

where psql >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo WARNING: psql not found in PATH.
    echo.
    echo Options:
    echo   1. Install PostgreSQL from https://www.postgresql.org/download/
    echo   2. Add PostgreSQL bin folder to your PATH
    echo   3. Skip this and just run start.bat (uses in-memory data)
    echo.
    pause
    exit /b 1
)

:: Create DB
echo Creating database yuktira_erp...
psql -h %PGHOST% -p %PGPORT% -U %PGUSER% -c "CREATE DATABASE yuktira_erp;" 2>nul

:: Load schema
echo Loading schema...
psql -h %PGHOST% -p %PGPORT% -U %PGUSER% -d yuktira_erp -f "database\scripts\001_core_schema.sql"
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Schema load failed!
    pause
    exit /b 1
)

:: Load migrations
echo Applying migrations...
psql -h %PGHOST% -p %PGPORT% -U %PGUSER% -d yuktira_erp -f "database\scripts\002_refresh_tokens.sql"
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Migration 002 failed!
    pause
    exit /b 1
)
psql -h %PGHOST% -p %PGPORT% -U %PGUSER% -d yuktira_erp -f "database\scripts\003_system_config.sql"
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Migration 003 failed!
    pause
    exit /b 1
)
psql -h %PGHOST% -p %PGPORT% -U %PGUSER% -d yuktira_erp -f "database\scripts\004_mrp_extensions.sql"
psql -h %PGHOST% -p %PGPORT% -U %PGUSER% -d yuktira_erp -f "database\scripts\005_bi_extensions.sql"
psql -h %PGHOST% -p %PGPORT% -U %PGUSER% -d yuktira_erp -f "database\scripts\006_integration_queue.sql"

:: Load seed data
echo Loading sample data...
psql -h %PGHOST% -p %PGPORT% -U %PGUSER% -d yuktira_erp -f "database\seed\01_sample_data.sql"
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Sample data load failed!
    pause
    exit /b 1
)

echo.
echo Database initialized: yuktira_erp (60+ tables, sample data loaded)
pause

@echo off
cd /d "%~dp0\YuktiraERP"
title Yuktira ERP - Database Installer

echo ============================================
echo   YUKTIRA ERP - DATABASE INSTALLATION
echo ============================================
echo.
echo  This script installs the Yuktira ERP database
echo  on a PostgreSQL server.
echo.

REM === CHECK POSTGRESQL ===
where psql >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo  PostgreSQL (psql) not found. Install it first:
    echo    https://www.postgresql.org/download/
    echo.
    echo  After installing, ensure the bin folder is in your PATH,
    echo  or run this script from "C:\Program Files\PostgreSQL\16\bin\"
    echo.
    pause
    exit /b 1
)

echo  PostgreSQL found.
echo.

REM === GET CONNECTION DETAILS ===
set /p PGHOST="Enter PostgreSQL host [localhost]: "
if "%PGHOST%"=="" set PGHOST=localhost

set /p PGPORT="Enter PostgreSQL port [5432]: "
if "%PGPORT%"=="" set PGPORT=5432

set /p PGUSER="Enter PostgreSQL username [postgres]: "
if "%PGUSER%"=="" set PGUSER=postgres

echo.
echo  Connecting to %PGHOST%:%PGPORT% as %PGUSER%...
echo.

REM === TEST CONNECTION ===
psql -h %PGHOST% -p %PGPORT% -U %PGUSER% -c "SELECT 1;" >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo  ERROR: Could not connect to PostgreSQL.
    echo.
    echo  Check that:
    echo    1. PostgreSQL service is running
    echo    2. Host, port, and username are correct
    echo    3. PostgreSQL allows connections (check pg_hba.conf)
    echo.
    pause
    exit /b 1
)
echo  Connection successful.
echo.

REM === CREATE DATABASE ===
echo  Step 1/4 - Creating database "yuktira_erp"...
psql -h %PGHOST% -p %PGPORT% -U %PGUSER% -c "CREATE DATABASE yuktira_erp;" 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo  Database may already exist. Continuing...
) else (
    echo  Database created.
)
echo.

REM === LOAD SCHEMA ===
echo  Step 2/4 - Loading core schema...
psql -h %PGHOST% -p %PGPORT% -U %PGUSER% -d yuktira_erp -f "database\scripts\001_core_schema.sql"
if %ERRORLEVEL% NEQ 0 (
    echo  ERROR: Schema load failed!
    pause
    exit /b 1
)
echo  Core schema loaded.
echo.

REM === APPLY MIGRATIONS ===
echo  Step 3/4 - Applying migrations...

set SCRIPTS=database\scripts
for %%f in ("%SCRIPTS%\002_refresh_tokens.sql" "%SCRIPTS%\003_system_config.sql" "%SCRIPTS%\004_mrp_extensions.sql" "%SCRIPTS%\005_bi_extensions.sql" "%SCRIPTS%\006_integration_queue.sql") do (
    echo    Running %%~nxf ...
    psql -h %PGHOST% -p %PGPORT% -U %PGUSER% -d yuktira_erp -f "%%f"
    if %ERRORLEVEL% NEQ 0 (
        echo  ERROR: Migration %%~nxf failed!
        pause
        exit /b 1
    )
)
echo  All migrations applied.
echo.

REM === LOAD SEED DATA ===
echo  Step 4/4 - Loading sample data...
if exist "database\seed\01_sample_data.sql" (
    psql -h %PGHOST% -p %PGPORT% -U %PGUSER% -d yuktira_erp -f "database\seed\01_sample_data.sql"
    if %ERRORLEVEL% NEQ 0 (
        echo  WARNING: Sample data load had issues (may be optional).
    ) else (
        echo  Sample data loaded.
    )
) else (
    echo  No seed file found, skipping sample data.
)
echo.

REM === VERIFY ===
echo  Verifying installation...
psql -h %PGHOST% -p %PGPORT% -U %PGUSER% -d yuktira_erp -c "SELECT table_name FROM information_schema.tables WHERE table_schema LIKE 'yuktira_%%' ORDER BY table_name;" 2>nul
echo.

echo ============================================
echo   DATABASE INSTALLATION COMPLETE
echo ============================================
echo.
echo  Database: yuktira_erp
echo  Host:     %PGHOST%:%PGPORT%
echo  User:     %PGUSER%
echo.
echo  Next step:
echo    Update your appsettings.json:
echo      \"ConnectionStrings\": {
echo        \"YuktiraDb\": \"Host=%PGHOST%;Port=%PGPORT%;Database=yuktira_erp;Username=%PGUSER%;Password=YourPassword\"
echo      }
echo.
echo  Then run: server.bat  (or start.bat)
echo.
pause

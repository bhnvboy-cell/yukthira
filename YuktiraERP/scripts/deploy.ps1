# ============================================
# YUKTIRA ERP SUITE - Deployment Script
# ============================================

param(
    [string]$Environment = "dev",
    [switch]$Build,
    [switch]$Run,
    [switch]$Stop,
    [switch]$Restart,
    [switch]$InitDb
)

$RootDir = Split-Path -Parent $PSScriptRoot
$ComposeFile = Join-Path $RootDir "scripts\docker-compose.yml"

function Show-Status { Write-Host "[Yuktira] $($args[0])" -ForegroundColor Cyan }

function Invoke-DbInit {
    Show-Status "Initializing PostgreSQL database..."
    docker exec -i yuktira-postgres psql -U yuktira_admin -d yuktira_erp < (Join-Path $RootDir "database\scripts\001_core_schema.sql")
    Show-Status "Database initialization complete."
}

function Invoke-Build {
    Show-Status "Building solution..."
    & "$RootDir\scripts\build.ps1"
    Show-Status "Build complete."
}

function Invoke-Run {
    Show-Status "Starting Yuktira ERP Suite..."
    docker-compose -f $ComposeFile -p "yuktira" up -d
    Show-Status "Yuktira ERP Suite is running:"
    Show-Status "  Web UI:     http://localhost:5001"
    Show-Status "  API:        http://localhost:5000/swagger"
    Show-Status "  PostgreSQL: localhost:5432"
}

function Invoke-Stop {
    Show-Status "Stopping Yuktira ERP Suite..."
    docker-compose -f $ComposeFile -p "yuktira" down
    Show-Status "Yuktira ERP Suite stopped."
}

function Invoke-Restart {
    Invoke-Stop
    Start-Sleep -Seconds 3
    Invoke-Run
}

# Main execution
try {
    if ($Stop) { Invoke-Stop; return }
    if ($Restart) { Invoke-Restart; return }
    if ($Build) { Invoke-Build }
    if ($InitDb) { Invoke-DbInit }
    if ($Run) { Invoke-Run }

    if (-not $Build -and -not $Run -and -not $Stop -and -not $Restart -and -not $InitDb) {
        Show-Status "Usage: .\deploy.ps1 [-Build] [-Run] [-Stop] [-Restart] [-InitDb]"
        Show-Status "  -Build   Build the .NET solution"
        Show-Status "  -Run     Start all services with Docker Compose"
        Show-Status "  -Stop    Stop all services"
        Show-Status "  -Restart Stop then start"
        Show-Status "  -InitDb  Initialize database schema"
    }
}
catch {
    Write-Host "[ERROR] $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

param(
    [string]$Database = "yuktira_erp",
    [string]$Server = "127.0.0.1",
    [string]$Port = "5432",
    [string]$Username = "postgres",
    [string]$Password,
    [string]$BackupFile
)

# Locate PostgreSQL client tools (bin path may not be on PATH)
$pgBin = $null
if (Get-Command "pg_restore.exe" -ErrorAction SilentlyContinue) {
    $pgBin = ""
} else {
    $candidates = @(
        "C:\Program Files\PostgreSQL\18\bin",
        "C:\Program Files\PostgreSQL\17\bin",
        "C:\Program Files\PostgreSQL\16\bin",
        "C:\Program Files\PostgreSQL\15\bin",
        "C:\Program Files\PostgreSQL\14\bin",
        "C:\Program Files\PostgreSQL\13\bin"
    )
    foreach ($c in $candidates) {
        if (Test-Path (Join-Path $c "pg_restore.exe")) { $pgBin = $c; break }
    }
}
if ($null -eq $pgBin) {
    Write-Host "pg_restore not found. Install PostgreSQL or add its bin folder to PATH." -ForegroundColor Red
    exit 1
}

if (-not $BackupFile) {
    $backupDir = ".\database\backup"
    $latest = Get-ChildItem -Path $backupDir -Filter "*.sql" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $latest) { Write-Host "No backup files found in $backupDir" -ForegroundColor Red; exit 1 }
    $BackupFile = $latest.FullName
}

if (-not (Test-Path -Path $BackupFile)) { Write-Host "Backup file not found: $BackupFile" -ForegroundColor Red; exit 1 }

Write-Host "WARNING: This will OVERWRITE database $Database with $BackupFile"
$confirm = Read-Host "Are you sure? (y/N)"
if ($confirm -ne "y") { Write-Host "Cancelled."; exit 0 }

Write-Host "Restoring $BackupFile to $Database ..."
if ($Password) { $env:PGPASSWORD = $Password }
$pgRestore = if ($pgBin) { Join-Path $pgBin "pg_restore.exe" } else { "pg_restore.exe" }
& $pgRestore --host=$Server --port=$Port --username=$Username --dbname=$Database --clean --if-exists $BackupFile
if ($LASTEXITCODE -eq 0) {
    Write-Host "Restore completed successfully"
} else {
    Write-Host "Restore FAILED" -ForegroundColor Red
    exit 1
}
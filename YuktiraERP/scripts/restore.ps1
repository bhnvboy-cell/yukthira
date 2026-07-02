param(
    [string]$Database = "yuktira_erp",
    [string]$Host = "localhost",
    [string]$Port = "5432",
    [string]$Username = "yuktira_admin",
    [string]$BackupFile
)

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
$env:PGPASSWORD = "<your-db-password>"
pg_restore --host=$Host --port=$Port --username=$Username --dbname=$Database --clean --if-exists $BackupFile
if ($LASTEXITCODE -eq 0) {
    Write-Host "Restore completed successfully"
} else {
    Write-Host "Restore FAILED" -ForegroundColor Red
    exit 1
}

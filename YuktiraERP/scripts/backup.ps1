param(
    [string]$Database = "yuktira_erp",
    [string]$Host = "localhost",
    [string]$Port = "5432",
    [string]$Username = "yuktira_admin",
    [string]$BackupDir = ".\database\backup"
)

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFile = Join-Path -Path $BackupDir -ChildPath "${Database}_${timestamp}.sql"

if (-not (Test-Path -Path $BackupDir)) { New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null }

Write-Host "Backing up $Database to $backupFile ..."
$env:PGPASSWORD = "<your-db-password>"
pg_dump --host=$Host --port=$Port --username=$Username --format=custom --file=$backupFile $Database
if ($LASTEXITCODE -eq 0) {
    Write-Host "Backup completed: $backupFile"
    Write-Host "Size: $((Get-Item $backupFile).Length / 1MB) MB"
} else {
    Write-Host "Backup FAILED" -ForegroundColor Red
    exit 1
}

# Rotate: keep last 30 backups
$all = Get-ChildItem -Path $BackupDir -Filter "*.sql" | Sort-Object LastWriteTime -Descending
if ($all.Count -gt 30) {
    $all | Select-Object -Skip 30 | Remove-Item -Force
    Write-Host "Removed $($all.Count - 30) old backup(s)"
}

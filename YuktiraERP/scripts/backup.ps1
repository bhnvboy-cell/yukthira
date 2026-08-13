param(
    [string]$Database = "yuktira_erp",
    [string]$Server = "127.0.0.1",
    [string]$Port = "5432",
    [string]$Username = "postgres",
    [string]$Password,
    [string]$BackupDir = ".\database\backup",
    [int]$Keep = 30
)

# Locate PostgreSQL client tools (bin path may not be on PATH)
$pgBin = $null
if (Get-Command "pg_dump.exe" -ErrorAction SilentlyContinue) {
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
        if (Test-Path (Join-Path $c "pg_dump.exe")) { $pgBin = $c; break }
    }
}
if ($null -eq $pgBin) {
    Write-Host "pg_dump not found. Install PostgreSQL or add its bin folder to PATH." -ForegroundColor Red
    exit 1
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFile = Join-Path -Path $BackupDir -ChildPath "${Database}_${timestamp}.sql"

if (-not (Test-Path -Path $BackupDir)) { New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null }

Write-Host "Backing up $Database to $backupFile ..."
if ($Password) { $env:PGPASSWORD = $Password }
$pgDump = if ($pgBin) { Join-Path $pgBin "pg_dump.exe" } else { "pg_dump.exe" }
& $pgDump --host=$Server --port=$Port --username=$Username --format=custom --file=$backupFile $Database
if ($LASTEXITCODE -eq 0) {
    Write-Host "Backup completed: $backupFile"
    Write-Host "Size: $([math]::Round((Get-Item $backupFile).Length / 1MB, 2)) MB"
} else {
    Write-Host "Backup FAILED" -ForegroundColor Red
    exit 1
}

# Rotate: keep last $Keep backups
$all = Get-ChildItem -Path $BackupDir -Filter "*.sql" | Sort-Object LastWriteTime -Descending
if ($all.Count -gt $Keep) {
    $all | Select-Object -Skip $Keep | Remove-Item -Force
    Write-Host "Removed $($all.Count - $Keep) old backup(s)"
}
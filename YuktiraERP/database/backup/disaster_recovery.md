# Disaster Recovery & Backup Strategy

## Backup Types
| Type | Frequency | Retention | Tool |
|------|-----------|-----------|------|
| Full DB (custom format) | Daily at 2 AM | 30 days | `pg_dump` + `scripts/backup.ps1` |
| Transaction logs | Continuous (if WAL archiving enabled) | 7 days | PostgreSQL WAL |
| Configuration exports | On change | Last 10 versions | Manual / CI pipeline |
| Plugin binaries | On deploy | Last 5 versions | CI artifact store |

## Backup Command
```powershell
.\scripts\backup.ps1 -Database "yuktira_erp"
```
Defaults: connects to `127.0.0.1:5432` as `postgres` (matching the app's connection string), writes a custom-format dump to `.\database\backup`, keeps the last 30 backups. Pass `-Password` if your PostgreSQL requires one. PostgreSQL client tools are auto-located (searches `C:\Program Files\PostgreSQL\13-18\bin`).

## Restore Command
```powershell
.\scripts\restore.ps1 -Database "yuktira_erp" -BackupFile ".\database\backup\yuktira_erp_20240101_020000.sql"
```
Omitting `-BackupFile` restores the most recent backup. Restore prompts for confirmation before overwriting.

## Failover Procedure
1. Identify failure — check health endpoints: `GET /health`, `GET /api/health/ready`
2. Promote replica if using streaming replication: `pg_ctl promote`
3. Point application to standby via `appsettings.json` `ConnectionStrings.YuktiraDb`
4. Restore from latest backup if no replica: `.\scripts\restore.ps1`
5. Verify data integrity — run: `SELECT count(*) FROM information_schema.tables WHERE table_schema LIKE 'yuktira_%'`

## RPO / RTO Targets
| Metric | Target |
|--------|--------|
| Recovery Point Objective (RPO) | 24 hours (daily backup) |
| Recovery Time Objective (RTO) | 2 hours (restore from backup) |
| With streaming replica | RPO < 1 MB, RTO < 5 min |

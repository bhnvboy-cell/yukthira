@echo off
echo Stopping Yuktira ERP processes...
powershell -NoProfile -ExecutionPolicy Bypass -Command "$procs = Get-CimInstance Win32_Process | Where-Object { $_.Name -eq 'dotnet.exe' -and $_.CommandLine -match 'YuktiraERP' }; if (-not $procs) { Write-Host '  No running Yuktira ERP processes.' } else { $procs | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue; Write-Host ('  Stopped PID ' + $_.ProcessId) } }"
timeout /t 2 /nobreak >nul
echo Done.

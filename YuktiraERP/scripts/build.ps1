# ============================================
# YUKTIRA ERP SUITE - Build Script
# ============================================

$RootDir = Split-Path -Parent $PSScriptRoot
$SolutionFile = Join-Path $RootDir "YuktiraERP.sln"

Write-Host "[Yuktira] Restoring NuGet packages..." -ForegroundColor Cyan
dotnet restore $SolutionFile

Write-Host "[Yuktira] Building solution (Release)..." -ForegroundColor Cyan
dotnet build $SolutionFile -c Release --no-restore

Write-Host "[Yuktira] Running tests (if any)..." -ForegroundColor Cyan
$testProjects = Get-ChildItem -Path $RootDir -Recurse -Filter "*.Tests.csproj"
foreach ($tp in $testProjects) {
    dotnet test $tp.FullName -c Release --no-build
}

Write-Host "[Yuktira] Build complete." -ForegroundColor Green

#Requires -Version 5.1
# Sobe o ambiente de desenvolvimento local completo: Postgres + Redis (Docker), as duas APIs .NET
# (Nexora.Api.Edge e Nexora.Api.Cloud) e os 5 apps do frontend (turbo --parallel).
# Uso: pnpm dev:all  (a partir de Git/), ou diretamente: .\infra\scripts\dev-up.ps1

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $repoRoot

$connEdge = 'Host=localhost;Port=5432;Database=donabetinha_edge_dev;Username=donabetinha;Password=donabetinha_dev_only'
$connCloud = 'Host=localhost;Port=5432;Database=donabetinha_cloud_dev;Username=donabetinha;Password=donabetinha_dev_only'

Write-Host "==> Subindo Postgres e Redis (Docker)..." -ForegroundColor Cyan
docker compose -f infra/dev/docker-compose.yml up -d --wait

Write-Host "==> Aplicando migrations EF Core (Nexora.Api.Edge)..." -ForegroundColor Cyan
$env:NEXORA_MIGRATIONS_CONNECTION = $connEdge
dotnet ef database update --project backend/src/Nexora.Infrastructure

Write-Host "==> Aplicando migrations EF Core (Nexora.Api.Cloud)..." -ForegroundColor Cyan
$env:NEXORA_MIGRATIONS_CONNECTION = $connCloud
dotnet ef database update --project backend/src/Nexora.Infrastructure
Remove-Item Env:\NEXORA_MIGRATIONS_CONNECTION

Write-Host "==> Subindo Nexora.Api.Edge em http://localhost:5000 ..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList @(
  '-NoExit', '-Command',
  "Set-Location '$repoRoot'; `$env:ASPNETCORE_ENVIRONMENT='Development'; `$env:ASPNETCORE_URLS='http://localhost:5000'; dotnet run --project backend/src/Nexora.Api.Edge"
)

Write-Host "==> Subindo Nexora.Api.Cloud em http://localhost:5100 ..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList @(
  '-NoExit', '-Command',
  "Set-Location '$repoRoot'; `$env:ASPNETCORE_ENVIRONMENT='Development'; `$env:ASPNETCORE_URLS='http://localhost:5100'; dotnet run --project backend/src/Nexora.Api.Cloud"
)

if (-not (Test-Path (Join-Path $repoRoot 'node_modules'))) {
  Write-Host "==> node_modules ausente, rodando pnpm install..." -ForegroundColor Cyan
  pnpm install
}

Write-Host "==> Subindo os 5 apps do frontend (pnpm dev / turbo --parallel)..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList @(
  '-NoExit', '-Command',
  "Set-Location '$repoRoot'; pnpm dev"
)

Write-Host ''
Write-Host 'Ambiente completo no ar (cada serviço em sua própria janela):' -ForegroundColor Green
Write-Host '  Postgres          -> localhost:5432 (donabetinha_edge_dev / donabetinha_cloud_dev)'
Write-Host '  Redis             -> localhost:6379'
Write-Host '  Nexora.Api.Edge   -> http://localhost:5000/swagger'
Write-Host '  Nexora.Api.Cloud  -> http://localhost:5100/swagger'
Write-Host '  Frontend (5 apps) -> portas do Vite exibidas na janela do pnpm dev (padrão a partir de 5173)'

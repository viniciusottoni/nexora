#Requires -Version 5.1
# Sobe o ambiente de desenvolvimento local completo: Postgres + Redis (Docker), as duas APIs .NET
# (Nexora.Api.Edge e Nexora.Api.Cloud) e os 5 apps do frontend (turbo --parallel).
# Uso: pnpm dev:all  (a partir de Git/), ou diretamente: .\infra\scripts\dev-up.ps1

$ErrorActionPreference = 'Stop'

function Test-PortListening {
  param([int]$Port)
  return [bool](Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue)
}

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $repoRoot

$connEdge = 'Host=localhost;Port=5432;Database=donabetinha_edge_dev;Username=donabetinha;Password=donabetinha_dev_only'
$connCloud = 'Host=localhost;Port=5432;Database=donabetinha_cloud_dev;Username=donabetinha;Password=donabetinha_dev_only'

Write-Host "==> Subindo Postgres e Redis (Docker)..." -ForegroundColor Cyan
docker compose -f infra/dev/docker-compose.yml up -d --wait

Write-Host "==> Build seguro do backend (.NET)..." -ForegroundColor Cyan
dotnet build backend/Nexora.slnx -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false

Write-Host "==> Aplicando migrations EF Core (Nexora.Api.Edge)..." -ForegroundColor Cyan
$env:NEXORA_MIGRATIONS_CONNECTION = $connEdge
dotnet ef database update --no-build --project backend/src/Nexora.Infrastructure

Write-Host "==> Aplicando migrations EF Core (Nexora.Api.Cloud)..." -ForegroundColor Cyan
$env:NEXORA_MIGRATIONS_CONNECTION = $connCloud
dotnet ef database update --no-build --project backend/src/Nexora.Infrastructure
Remove-Item Env:\NEXORA_MIGRATIONS_CONNECTION

Write-Host "==> Criando massa de usuários de teste (Cloud + Edge)..." -ForegroundColor Cyan
dotnet run --no-build --project backend/src/Nexora.DevSeeder -- --connection $connCloud --mode cloud
dotnet run --no-build --project backend/src/Nexora.DevSeeder -- --connection $connEdge --mode edge

if (Test-PortListening -Port 5000) {
  Write-Host "==> Nexora.Api.Edge já está rodando em http://localhost:5000, pulando." -ForegroundColor Yellow
} else {
  Write-Host "==> Subindo Nexora.Api.Edge em http://localhost:5000 ..." -ForegroundColor Cyan
  Start-Process powershell -ArgumentList @(
    '-NoExit', '-Command',
    "Set-Location '$repoRoot'; `$env:ASPNETCORE_ENVIRONMENT='Development'; `$env:ASPNETCORE_URLS='http://localhost:5000'; dotnet run --no-build --project backend/src/Nexora.Api.Edge"
  )
}

if (Test-PortListening -Port 5100) {
  Write-Host "==> Nexora.Api.Cloud já está rodando em http://localhost:5100, pulando." -ForegroundColor Yellow
} else {
  Write-Host "==> Subindo Nexora.Api.Cloud em http://localhost:5100 ..." -ForegroundColor Cyan
  Start-Process powershell -ArgumentList @(
    '-NoExit', '-Command',
    "Set-Location '$repoRoot'; `$env:ASPNETCORE_ENVIRONMENT='Development'; `$env:ASPNETCORE_URLS='http://localhost:5100'; dotnet run --no-build --project backend/src/Nexora.Api.Cloud"
  )
}

if (-not (Test-Path (Join-Path $repoRoot 'node_modules'))) {
  Write-Host "==> node_modules ausente, rodando pnpm install..." -ForegroundColor Cyan
  pnpm install
}

$frontendPorts = 5173, 5174, 5175, 5176, 5177
if (($frontendPorts | Where-Object { Test-PortListening -Port $_ }).Count -eq $frontendPorts.Count) {
  Write-Host "==> Os 5 apps do frontend já estão rodando (portas 5173-5177), pulando." -ForegroundColor Yellow
} else {
  Write-Host "==> Subindo os 5 apps do frontend (pnpm dev / turbo --parallel)..." -ForegroundColor Cyan
  Start-Process powershell -ArgumentList @(
    '-NoExit', '-Command',
    "Set-Location '$repoRoot'; pnpm dev"
  )
}

Write-Host ''
Write-Host 'Ambiente completo no ar (cada serviço em sua própria janela):' -ForegroundColor Green
Write-Host '  Postgres          -> localhost:5432 (donabetinha_edge_dev / donabetinha_cloud_dev)'
Write-Host '  Redis             -> localhost:6379'
Write-Host '  Nexora.Api.Edge   -> http://localhost:5000/swagger'
Write-Host '  Nexora.Api.Cloud  -> http://localhost:5100/swagger'
Write-Host '  Gestão local      -> http://localhost:5173/admin'
Write-Host '  Plataforma        -> http://localhost:5174'
Write-Host '  Caixa (POS)       -> http://localhost:5175'
Write-Host '  Cozinha (KDS)     -> http://localhost:5176'
Write-Host '  Cardápio          -> http://localhost:5177'
Write-Host ''
Write-Host 'Primeiro pareamento:' -ForegroundColor Yellow
Write-Host '  1. Abra http://localhost:5173/admin e use o código inicial exibido pelo seeder.'
Write-Host '  2. Entre com o PIN 2101 (proprietário) ou 2102 (gerente).'
Write-Host '  3. Clique em "Autorizar novo dispositivo" e use o novo código no Caixa/KDS.'

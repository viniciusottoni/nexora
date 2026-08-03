#Requires -Version 5.1
# Renova o código inicial de pareamento no banco Edge sem reiniciar APIs ou frontends.

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$edgeConnection = 'Host=localhost;Port=5432;Database=donabetinha_edge_dev;Username=donabetinha;Password=donabetinha_dev_only'

Set-Location $repoRoot

Write-Host '==> Renovando o código inicial da gestão local...' -ForegroundColor Cyan
dotnet run --project backend/src/Nexora.DevSeeder -- --connection $edgeConnection --mode edge

if ($LASTEXITCODE -ne 0) {
  throw "O DevSeeder terminou com o código $LASTEXITCODE."
}

Write-Host ''
Write-Host 'Abra http://localhost:5173/admin e use o código exibido acima.' -ForegroundColor Green

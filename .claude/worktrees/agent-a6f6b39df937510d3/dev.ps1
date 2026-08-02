#requires -Version 5.1
<#
  Sobe tudo que e necessario para testar o AWAKEN localmente:
    1. Postgres + Redis (docker compose)
    2. Backend ASP.NET Core (dotnet run), escutando em 0.0.0.0 para aceitar conexoes da rede local
    3. App web React (Vite), com area admin e proxy para o backend local
    4. Flutter app (flutter run), apontando para o IP da maquina na rede local

  Uso:
    .\dev.ps1                 # detecta automaticamente o device do `flutter devices`
    .\dev.ps1 -DeviceId <id>  # forca um device especifico (ex: emulator-5554)
    .\dev.ps1 -BackendPort 5219
    .\dev.ps1 -WebPort 3100
#>

param(
    [string]$DeviceId,
    [int]$BackendPort = 5219,
    [Alias("AdminPort")]
    [int]$WebPort = 3100
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$backendDir = Join-Path $root "backend\src\Awaken.Api"
$mobileDir = Join-Path $root "apps\mobile"
$webDir = Join-Path $root "apps\web"

# 1. Infra (Postgres + Redis)
Write-Host "==> Subindo Postgres + Redis (docker compose)..." -ForegroundColor Cyan
docker compose -f (Join-Path $root "docker-compose.yml") up -d
if ($LASTEXITCODE -ne 0) {
    throw "Falha ao subir docker compose. Verifique se o Docker Desktop esta rodando."
}

# 2. Descobrir IP da maquina na rede local (para o celular acessar o backend)
Write-Host "==> Detectando IP da rede local..." -ForegroundColor Cyan
$lanIp = (
    Get-NetIPAddress -AddressFamily IPv4 |
    Where-Object {
        $_.IPAddress -notlike "127.*" -and
        $_.IPAddress -notlike "169.254.*" -and
        $_.InterfaceAlias -notmatch "Loopback|vEthernet|WSL|Virtual"
    } |
    Select-Object -First 1 -ExpandProperty IPAddress
)

if (-not $lanIp) {
    throw "Nao foi possivel detectar o IP da rede local. Conecte-se ao Wi-Fi/Ethernet e tente novamente."
}
Write-Host "    IP local: $lanIp" -ForegroundColor Green

# 3. Backend - sobe em uma janela separada, escutando em todas as interfaces
Write-Host "==> Iniciando backend (dotnet run) em http://0.0.0.0:$BackendPort ..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList @(
    "-NoExit", "-Command",
    "Set-Location '$backendDir'; dotnet run --urls http://0.0.0.0:$BackendPort"
)

Write-Host "    Lembre-se: libere o firewall do Windows para a porta $BackendPort na primeira execucao (sera solicitado)." -ForegroundColor Yellow
Write-Host "    Aguardando o backend inicializar..." -ForegroundColor Cyan
Start-Sleep -Seconds 6

# 4. App web React - sobe em uma janela separada, proxyando /api para o backend local
$webApiProxyTarget = "http://localhost:$BackendPort"
Write-Host "==> Iniciando app web React (Vite) em http://localhost:$WebPort ..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList @(
    "-NoExit", "-Command",
    "Set-Location '$webDir'; `$env:VITE_API_PROXY_TARGET='$webApiProxyTarget'; npm run dev -- --host 0.0.0.0 --port $WebPort"
)
Write-Host "    Web: http://localhost:$WebPort (proxy /api -> $webApiProxyTarget)" -ForegroundColor Green

# 5. Flutter - roda no terminal atual, apontando para o IP local
$apiUrl = "http://${lanIp}:${BackendPort}"
Write-Host "==> Iniciando Flutter (flutter run), API_BASE_URL=$apiUrl ..." -ForegroundColor Cyan
Set-Location $mobileDir

$flutterArgs = @("run", "--dart-define=API_BASE_URL=$apiUrl")
if ($DeviceId) {
    $flutterArgs += @("-d", $DeviceId)
}

& flutter @flutterArgs

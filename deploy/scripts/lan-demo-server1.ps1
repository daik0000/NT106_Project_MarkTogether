param(
    [string]$DbPassword = "marktogether_demo_123",
    [string]$CertPassword = "ChangeMe-DEV"
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$serverDir = Join-Path $repoRoot "MarkTogether.Server\bin\Release"
$serverExe = Join-Path $serverDir "MarkTogether.Server.exe"
$certPath = Join-Path $repoRoot "certs\app.pfx"
$storagePath = Join-Path $repoRoot "lan-demo\storage\shared"

if (!(Test-Path $serverExe)) {
    throw "Missing $serverExe. Run: dotnet build MarkTogether.sln -c Release /m:1"
}
if (!(Test-Path $certPath)) {
    throw "Missing $certPath. Run deploy/scripts/lan-demo-create-certs.ps1 first."
}

New-Item -ItemType Directory -Force -Path $storagePath | Out-Null

$env:MARKTOGETHER_PORT = "5101"
$env:MARKTOGETHER_LISTEN_HOST = "127.0.0.1"
$env:MARKTOGETHER_STORAGE_PATH = $storagePath
$env:MARKTOGETHER_TLS_CERT_PATH = $certPath
$env:MARKTOGETHER_TLS_CERT_PASSWORD = $CertPassword
$env:MARKTOGETHER_DB_CONNECTION = "Host=127.0.0.1;Port=15432;Database=marktogether_db;Username=marktogether_user;Password=$DbPassword"
$env:MARKTOGETHER_REDIS_CONNECTION = "127.0.0.1:16379,password=$DbPassword,ssl=false,abortConnect=false"
$env:MARKTOGETHER_SESSION_TTL_SECONDS = "86400"

Write-Host "[LAN demo] Starting app server #1 on 127.0.0.1:5101"
Push-Location $serverDir
try {
    & $serverExe --service
}
finally {
    Pop-Location
}

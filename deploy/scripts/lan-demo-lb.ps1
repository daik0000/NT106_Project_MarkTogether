param(
    [string]$CertPassword = "ChangeMe-DEV"
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$lbDir = Join-Path $repoRoot "LoadBalancing\MarkTogether.Gateway\bin\Release"
$lbExe = Join-Path $lbDir "MarkTogether.Gateway.exe"
$certPath = Join-Path $repoRoot "certs\lb.pfx"

if (!(Test-Path $lbExe)) {
    throw "Missing $lbExe. Run: dotnet build MarkTogether.sln -c Release /m:1"
}
if (!(Test-Path $certPath)) {
    throw "Missing $certPath. Run deploy/scripts/lan-demo-create-certs.ps1 first."
}

$env:MARKTOGETHER_LB_LISTEN_HOST = "0.0.0.0"
$env:MARKTOGETHER_LB_LISTEN_PORT = "5000"
$env:MARKTOGETHER_LB_CERT_PATH = $certPath
$env:MARKTOGETHER_LB_CERT_PASSWORD = $CertPassword
$env:MARKTOGETHER_LB_BACKENDS = "127.0.0.1:5101,127.0.0.1:5102"
$env:MARKTOGETHER_LB_UPSTREAM_TLS = "true"
$env:MARKTOGETHER_LB_HEALTHCHECK_INTERVAL_SECONDS = "3"
$env:MARKTOGETHER_LB_DOC_REMAP_TIMEOUT_SECONDS = "30"
$env:MARKTOGETHER_LB_CONNECT_TIMEOUT_MS = "3000"

Write-Host "[LAN demo] Starting LB on 0.0.0.0:5000 -> 127.0.0.1:5101,127.0.0.1:5102"
Push-Location $lbDir
try {
    & $lbExe
}
finally {
    Pop-Location
}

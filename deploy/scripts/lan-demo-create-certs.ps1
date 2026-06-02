param(
    [Parameter(Mandatory = $true)]
    [string]$LanIp,

    [string]$Password = "ChangeMe-DEV"
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$certDir = Join-Path $repoRoot "certs"
New-Item -ItemType Directory -Force -Path $certDir | Out-Null

function Invoke-OpenSsl {
    param([string[]]$OpenSslArgs)

    & openssl @OpenSslArgs
    if ($LASTEXITCODE -ne 0) {
        throw "openssl failed: $($OpenSslArgs -join ' ')"
    }
}

$lbKey = Join-Path $certDir "lb.key"
$lbCrt = Join-Path $certDir "lb.crt"
$lbPfx = Join-Path $certDir "lb.pfx"
$appKey = Join-Path $certDir "app.key"
$appCrt = Join-Path $certDir "app.crt"
$appPfx = Join-Path $certDir "app.pfx"

Invoke-OpenSsl -OpenSslArgs @(
    "req", "-x509", "-newkey", "rsa:2048", "-nodes",
    "-keyout", $lbKey,
    "-out", $lbCrt,
    "-days", "365",
    "-subj", "/CN=$LanIp",
    "-addext", "subjectAltName=IP:$LanIp,IP:127.0.0.1,DNS:localhost"
)

Invoke-OpenSsl -OpenSslArgs @(
    "pkcs12", "-export",
    "-inkey", $lbKey,
    "-in", $lbCrt,
    "-out", $lbPfx,
    "-passout", "pass:$Password",
    "-certpbe", "PBE-SHA1-3DES",
    "-keypbe", "PBE-SHA1-3DES",
    "-macalg", "sha1"
)

Invoke-OpenSsl -OpenSslArgs @(
    "req", "-x509", "-newkey", "rsa:2048", "-nodes",
    "-keyout", $appKey,
    "-out", $appCrt,
    "-days", "365",
    "-subj", "/CN=localhost",
    "-addext", "subjectAltName=IP:127.0.0.1,DNS:localhost"
)

Invoke-OpenSsl -OpenSslArgs @(
    "pkcs12", "-export",
    "-inkey", $appKey,
    "-in", $appCrt,
    "-out", $appPfx,
    "-passout", "pass:$Password",
    "-certpbe", "PBE-SHA1-3DES",
    "-keypbe", "PBE-SHA1-3DES",
    "-macalg", "sha1"
)

$thumb = (& openssl x509 -in $lbCrt -noout -fingerprint -sha1)
$thumb = ($thumb -replace ".*=", "" -replace ":", "").Trim()

Write-Host "Created:"
Write-Host "  $lbPfx"
Write-Host "  $appPfx"
Write-Host ""
Write-Host "LB cert thumbprint for client server.config:"
Write-Host $thumb

param(
    [Parameter(Mandatory = $true)]
    [string]$LanIp,

    [string]$Thumbprint = ""
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$sourceConfig = Join-Path $repoRoot "MarkTogether.Client\server.config"
$releaseConfig = Join-Path $repoRoot "MarkTogether.Client\bin\Release\server.config"
$lbCrt = Join-Path $repoRoot "certs\lb.crt"

if ([string]::IsNullOrWhiteSpace($Thumbprint) -and (Test-Path $lbCrt)) {
    $raw = (& openssl x509 -in $lbCrt -noout -fingerprint -sha1)
    if ($LASTEXITCODE -eq 0) {
        $Thumbprint = ($raw -replace ".*=", "" -replace ":", "").Trim()
    }
}

$content = @"
HOST=$LanIp
PORT=5000
CERT_THUMB=$Thumbprint
"@

Set-Content -Path $sourceConfig -Value $content -Encoding ASCII
Write-Host "Updated $sourceConfig"

if (Test-Path (Split-Path $releaseConfig -Parent)) {
    Set-Content -Path $releaseConfig -Value $content -Encoding ASCII
    Write-Host "Updated $releaseConfig"
}

Write-Host ""
Write-Host "Client endpoint:"
Write-Host "  $LanIp:5000"
Write-Host "Cert thumb:"
Write-Host "  $Thumbprint"

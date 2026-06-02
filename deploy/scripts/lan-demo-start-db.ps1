param(
    [string]$ComposeFile = ""
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
if ([string]::IsNullOrWhiteSpace($ComposeFile)) {
    $ComposeFile = Join-Path $repoRoot "docker-compose.lan-demo.yml"
}

docker compose -f $ComposeFile up -d
docker compose -f $ComposeFile ps

# One-shot demo-machine setup for Quorum.
# Run this ONCE on a fresh laptop, from the repo root, in PowerShell:
#   .\tools\setup-demo.ps1
#
# It will:
#   1. check the .NET 9 SDK + Docker are installed
#   2. start a SQL Server 2022 container (or reuse a running one)
#   3. write Forum.Api/appsettings.Development.local.json (gitignored) with the
#      Docker connection string + a freshly generated JWT signing key
#   4. install the dotnet-ef tool if missing, then create/update the database
#   5. print the exact commands to run the two apps and seed demo data
#
# Safe to re-run: it reuses an existing container and won't overwrite an
# existing .local.json unless you pass -Force.
param(
    [string]$SaPassword = "Quorum_Demo2026!",
    [string]$ContainerName = "quorum-sql",
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
Set-Location $repoRoot

function Step($n, $msg) { Write-Host "`n[$n] $msg" -ForegroundColor Cyan }
function Ok($msg)       { Write-Host "    OK: $msg" -ForegroundColor Green }
function Warn($msg)     { Write-Host "    ! $msg" -ForegroundColor Yellow }

# ---------------------------------------------------------------- 1. prerequisites
Step 1 "Checking prerequisites"

$dotnet = (Get-Command dotnet -ErrorAction SilentlyContinue)
if (-not $dotnet) { throw "The .NET SDK is not installed. Get .NET 9 from https://dotnet.microsoft.com/download" }
$sdks = (dotnet --list-sdks) -join "`n"
if ($sdks -notmatch '(?m)^9\.') { throw "No .NET 9 SDK found. Installed:`n$sdks" }
Ok "dotnet $(dotnet --version)"

$docker = (Get-Command docker -ErrorAction SilentlyContinue)
if (-not $docker) { throw "Docker is not installed / not on PATH. Install Docker Desktop and start it." }
try { docker info *> $null } catch { throw "Docker is installed but not running. Start Docker Desktop, then re-run." }
Ok "docker is running"

# ---------------------------------------------------------------- 2. SQL container
Step 2 "Starting SQL Server 2022 container ($ContainerName)"

$existing = (docker ps -a --filter "name=^/$ContainerName$" --format "{{.Names}} {{.State}}")
if ($existing) {
    if ($existing -match 'running') {
        Ok "container already running"
    } else {
        docker start $ContainerName | Out-Null
        Ok "started existing container"
    }
} else {
    docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=$SaPassword" `
        -p 1433:1433 --name $ContainerName -d `
        mcr.microsoft.com/mssql/server:2022-latest | Out-Null
    Ok "created new container"
}

Write-Host "    waiting for SQL Server to accept connections..." -NoNewline
$connStr = "Server=localhost,1433;Database=master;User Id=sa;Password=$SaPassword;TrustServerCertificate=True;Connect Timeout=3"
$ready = $false
foreach ($i in 1..30) {
    try {
        $c = New-Object System.Data.SqlClient.SqlConnection $connStr
        $c.Open(); $c.Close()
        $ready = $true; break
    } catch { Write-Host "." -NoNewline; Start-Sleep -Seconds 2 }
}
Write-Host ""
if (-not $ready) { throw "SQL Server did not become ready in ~60s. Check 'docker logs $ContainerName'." }
Ok "SQL Server is accepting connections on localhost,1433"

# ---------------------------------------------------------------- 3. local config
Step 3 "Writing Forum.Api/appsettings.Development.local.json"

$localCfg = Join-Path $repoRoot "Forum.Api/appsettings.Development.local.json"
if ((Test-Path $localCfg) -and -not $Force) {
    Warn "already exists — leaving it as-is (pass -Force to regenerate)"
} else {
    $bytes = New-Object byte[] 48
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    $jwtKey = [Convert]::ToBase64String($bytes)
    $appConn = "Server=localhost,1433;Database=ForumDb;User Id=sa;Password=$SaPassword;TrustServerCertificate=True"
    $json = [ordered]@{
        ConnectionStrings = [ordered]@{ DefaultConnection = $appConn }
        Jwt               = [ordered]@{ Key = $jwtKey }
    } | ConvertTo-Json -Depth 5
    Set-Content -Path $localCfg -Value $json -Encoding UTF8
    Ok "wrote connection string + a fresh 64-char JWT key (gitignored, never committed)"
}

# ---------------------------------------------------------------- 4. database
Step 4 "Applying database migrations"

$efInstalled = (dotnet tool list --global 2>$null) -match 'dotnet-ef'
if (-not $efInstalled) {
    Write-Host "    installing dotnet-ef tool..."
    dotnet tool install --global dotnet-ef | Out-Null
    Ok "dotnet-ef installed"
}
dotnet ef database update --project Forum.Api --startup-project Forum.Api | Out-Null
Ok "database ForumDb created / up to date"

# ---------------------------------------------------------------- 5. next steps
Step 5 "Setup complete — next steps"
$seedConn = "Server=localhost,1433;Database=ForumDb;User Id=sa;Password=$SaPassword;TrustServerCertificate=True"
Write-Host @"

  Open TWO terminals and run one command in each:

    dotnet run --project Forum.Api --launch-profile https
    dotnet run --project Forum.Web --launch-profile https

  Then, in a THIRD terminal, seed the demo content:

    .\tools\seed-demo.ps1 -ConnectionString "$seedConn"

  Open the site:   https://localhost:7225
  API docs:        https://localhost:7294/scalar

  Demo logins:
    admin    / Admin_2026!   (moderator)
    alex_dev / Demo_2026!
    sara_m   / Demo_2026!
"@ -ForegroundColor White

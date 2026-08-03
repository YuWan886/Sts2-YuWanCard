<#
.SYNOPSIS
  Builds the multi-version YuWanCard bundle: one content variant per sts2.dll snapshot
  under -ApiRoot, plus a loader compiled against the OLDEST snapshot, assembled into the
  game's mods/YuWanCard folder.

.PARAMETER ApiRoot
  Directory containing one sub-folder per game version, each holding sts2.dll
  (and optionally sts2.xml). Example: F:\sts2-mod\sts2-versions with 0.107.1/ and 0.110.0/.

.PARAMETER Configuration
  Build configuration (default Release).

.PARAMETER Sts2Path
  Game install root. If empty, auto-detected from the Steam registry. Used to resolve
  the mods folder and the support DLLs (0Harmony / System.IO.Hashing / GodotSharp).

.PARAMETER Versions
  Optional filter: only build these compat targets (e.g. "0.107.1"). Empty = all snapshots.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools/build-variants.ps1 -ApiRoot F:\sts2-mod\sts2-versions
#>
param(
    [Parameter(Mandatory = $true)][string]$ApiRoot,
    [Parameter(Mandatory = $false)][string]$Configuration = 'Release',
    [Parameter(Mandatory = $false)][string]$Sts2Path = '',
    [Parameter(Mandatory = $false)][string[]]$Versions = @()
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

# ── Resolve game paths ────────────────────────────────────────────
if ([string]::IsNullOrWhiteSpace($Sts2Path)) {
    $uninstallKey = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 2868840'
    $installLocation = (Get-ItemProperty -Path $uninstallKey -ErrorAction SilentlyContinue).InstallLocation
    if (-not [string]::IsNullOrWhiteSpace($installLocation)) {
        $Sts2Path = $installLocation
    } else {
        $autoSteam = Join-Path $env:USERPROFILE 'AppData\Roaming\Valve\Steam'  # fallback below
        throw "Could not auto-detect the game. Pass -Sts2Path <game root>."
    }
}

$Sts2Path = (Resolve-Path $Sts2Path).Path
$Sts2DataDir = Join-Path $Sts2Path 'data_sts2_windows_x86_64'
$ModsPath = Join-Path $Sts2Path 'mods'
$modRoot = Join-Path $ModsPath 'YuWanCard'

if (-not (Test-Path $Sts2DataDir)) {
    throw "Expected game data dir not found: $Sts2DataDir"
}

# ── Discover snapshots ────────────────────────────────────────────
$snapshots = Get-ChildItem -Directory $ApiRoot | Where-Object { Test-Path (Join-Path $_.FullName 'sts2.dll') } | Sort-Object Name
if ($Versions.Count -gt 0) {
    $snapshots = $snapshots | Where-Object { $Versions -contains $_.Name }
}
if ($snapshots.Count -eq 0) {
    throw "No matching <version>/sts2.dll snapshots found under $ApiRoot" + $(if ($Versions.Count -gt 0) { " for: $($Versions -join ', ')" } else { '' })
}

Write-Host "Snapshots: $($snapshots.Name -join ', ')"
Write-Host "Deploy target: $modRoot"

# ── Prepare temp data dirs ────────────────────────────────────────
$tmp = Join-Path $repoRoot '.build-variants-tmp'
if (Test-Path $tmp) { Remove-Item -Recurse -Force $tmp }
New-Item -ItemType Directory -Force -Path $tmp | Out-Null

try {
    # Support DLLs shared across snapshots (game-version-independent for compilation).
    $supportFiles = @('0Harmony.dll', 'System.IO.Hashing.dll', 'GodotSharp.dll')

    foreach ($ver in $snapshots) {
        $verDataDir = Join-Path $tmp $ver.Name
        New-Item -ItemType Directory -Force -Path $verDataDir | Out-Null
        Copy-Item -Force (Join-Path $ver.FullName 'sts2.dll') (Join-Path $verDataDir 'sts2.dll')
        foreach ($f in $supportFiles) {
            $src = Join-Path $Sts2DataDir $f
            if (Test-Path $src) { Copy-Item -Force $src (Join-Path $verDataDir $f) }
        }
    }

# ── Build the loader against the OLDEST snapshot ─────────────────
$loaderProj = Join-Path $repoRoot 'YuWanCardCode\Loader\YuWanCardCode.Loader.csproj'
$oldestDataDir = Join-Path $tmp $snapshots[0].Name
Write-Host "Building loader against oldest snapshot $($snapshots[0].Name) ..."
& dotnet build $loaderProj -c $Configuration "/p:Sts2DataDir=$oldestDataDir"
if ($LASTEXITCODE -ne 0) { throw "Loader build failed (exit $LASTEXITCODE)." }

# ── Build each content variant ────────────────────────────────────
$contentProj = Join-Path $repoRoot 'YuWanCard.csproj'
foreach ($ver in $snapshots) {
    $verDataDir = Join-Path $tmp $ver.Name
    Write-Host "Building content variant $($ver.Name) ..."
    # BuildProjectReferences=false: the loader was already built above (oldest snapshot);
    # PckPackerEnabled=false: the shared .pck is produced by the normal dev build, not here.
    & dotnet build $contentProj -c $Configuration `
        "/p:Sts2DataDir=$verDataDir" `
        "/p:VariantTarget=$($ver.Name)" `
        "/p:BuildProjectReferences=false" `
        "/p:PckPackerEnabled=false" `
        "/t:Rebuild"
    if ($LASTEXITCODE -ne 0) { throw "Content variant $($ver.Name) build failed (exit $LASTEXITCODE)." }
}

# ── Final report ──────────────────────────────────────────────────
Write-Host "`nBundle layout:"
Get-ChildItem $modRoot | ForEach-Object { Write-Host "  $($_.Name)" }
if (Test-Path (Join-Path $modRoot 'lib')) {
    Get-ChildItem (Join-Path $modRoot 'lib') | ForEach-Object {
        $dll = Join-Path $_.FullName 'YuWanCard.Content.dll'
        $marker = Join-Path $_.FullName 'compat-target.txt'
        $target = if (Test-Path $marker) { (Get-Content $marker -Raw).Trim() } else { '?' }
        Write-Host "  lib/$($_.Name)  -> compat $target  dll=$([bool](Test-Path $dll))"
    }
}
Write-Host "`nDone."
} finally {
    if (Test-Path $tmp) { Remove-Item -Recurse -Force $tmp }
    Write-Host "Temporary build directory cleaned: $tmp"
}

param(
    [string]$Configuration = "Release",
    [switch]$Clean = $false,
    [switch]$DllOnly = $false,
    [switch]$PckOnly = $false
)

$ErrorActionPreference = "Stop"
$ProjectPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectName = Split-Path -Leaf $ProjectPath

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  YuWanCard Build Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$SteamLibraryPath = "F:/steam/steamapps"
$ModsPath = "$SteamLibraryPath/common/Slay the Spire 2/mods/$ProjectName"
$GodotPath = "D:/Godot_v4.5.1-stable_mono_win64/Godot_v4.5.1-stable_mono_win64.exe"

if ($Clean) {
    Write-Host "[1/4] Cleaning build artifacts..." -ForegroundColor Yellow
    Remove-Item -Path "$ProjectPath\.godot\mono\temp\bin" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path "$ProjectPath\.godot\mono\temp\obj" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path "$ProjectPath\bin" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path "$ProjectPath\obj" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "  Clean completed." -ForegroundColor Green
} else {
    Write-Host "[1/4] Skipping clean (use -Clean to clean first)" -ForegroundColor Gray
}

$totalTime = 0.0

if (-not $PckOnly) {
    Write-Host ""
    Write-Host "[2/4] Building DLL..." -ForegroundColor Yellow
    $buildStart = Get-Date
    dotnet build -c $Configuration /nologo /v:minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  Build failed!" -ForegroundColor Red
        exit 1
    }
    $buildTime = ((Get-Date) - $buildStart).TotalSeconds
    $totalTime += $buildTime
    Write-Host "  DLL build completed in $([math]::Round($buildTime, 1))s" -ForegroundColor Green

    $dllPath = "$ModsPath/$ProjectName.dll"
    if (Test-Path $dllPath) {
        $dllSize = (Get-Item $dllPath).Length / 1KB
        Write-Host "  Output: $ProjectName.dll ($([math]::Round($dllSize, 1)) KB)" -ForegroundColor Gray
    }
} else {
    Write-Host ""
    Write-Host "[2/4] Skipping DLL build (-PckOnly)" -ForegroundColor Gray
}

if (-not $DllOnly) {
    Write-Host ""
    Write-Host "[3/4] Exporting PCK..." -ForegroundColor Yellow
    $pckStart = Get-Date
    
    $pckOutputPath = "$ModsPath/$ProjectName.pck"
    
    $env:IsInnerGodotExport = "true"
    $env:MSBUILDDISABLENODEREUSE = "1"
    
    Push-Location $ProjectPath
    try {
        $process = Start-Process -FilePath $GodotPath -ArgumentList @(
            "--headless",
            "--export-pack",
            "BasicExport",
            $pckOutputPath
        ) -NoNewWindow -PassThru -Wait
    } finally {
        Pop-Location
    }
    
    Remove-Item Env:IsInnerGodotExport -ErrorAction SilentlyContinue
    Remove-Item Env:MSBUILDDISABLENODEREUSE -ErrorAction SilentlyContinue
    
    $pckTime = ((Get-Date) - $pckStart).TotalSeconds
    $totalTime += $pckTime
    Write-Host "  PCK export completed in $([math]::Round($pckTime, 1))s" -ForegroundColor Green

    if (Test-Path $pckOutputPath) {
        $pckSize = (Get-Item $pckOutputPath).Length / 1MB
        Write-Host "  Output: $ProjectName.pck ($([math]::Round($pckSize, 2)) MB)" -ForegroundColor Gray
    }
} else {
    Write-Host ""
    Write-Host "[3/4] Skipping PCK export (-DllOnly)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "[4/4] Verifying output files..." -ForegroundColor Yellow

$dllPath = "$ModsPath/$ProjectName.dll"
$pckPath = "$ModsPath/$ProjectName.pck"
$jsonPath = "$ModsPath/$ProjectName.json"

$allOk = $true

if (Test-Path $dllPath) {
    $dllSize = (Get-Item $dllPath).Length / 1KB
    Write-Host "  [OK] $ProjectName.dll ($([math]::Round($dllSize, 1)) KB)" -ForegroundColor Green
} else {
    Write-Host "  [MISSING] $ProjectName.dll" -ForegroundColor Red
    $allOk = $false
}

if (Test-Path $pckPath) {
    $pckSize = (Get-Item $pckPath).Length / 1MB
    Write-Host "  [OK] $ProjectName.pck ($([math]::Round($pckSize, 2)) MB)" -ForegroundColor Green
} else {
    Write-Host "  [MISSING] $ProjectName.pck" -ForegroundColor Red
    $allOk = $false
}

if (Test-Path $jsonPath) {
    Write-Host "  [OK] $ProjectName.json" -ForegroundColor Green
} else {
    Write-Host "  [MISSING] $ProjectName.json" -ForegroundColor Red
    $allOk = $false
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
if ($allOk) {
    Write-Host "  Build completed successfully!" -ForegroundColor Green
    Write-Host "  Total time: $([math]::Round($totalTime, 1))s" -ForegroundColor Green
    Write-Host "  Output: $ModsPath" -ForegroundColor Gray
} else {
    Write-Host "  Build completed with errors!" -ForegroundColor Red
    exit 1
}
Write-Host "========================================" -ForegroundColor Cyan

# Build script for PoCX and Bitcoin-PoCX dependencies (Windows)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $ScriptDir

Write-Host "==> Building PoCX dependencies..." -ForegroundColor Green

# Build PoCX (Rust)
if (Test-Path "pocx") {
    Write-Host "==> Building PoCX binaries..." -ForegroundColor Green
    Set-Location pocx
    
    # Check if Rust is installed
    if (-not (Get-Command cargo -ErrorAction SilentlyContinue)) {
        Write-Host "Error: Rust/Cargo not found. Please install Rust from https://rustup.rs/" -ForegroundColor Red
        exit 1
    }
    
    # Check for nightly toolchain
    $toolchains = rustup toolchain list
    if ($toolchains -notmatch "nightly") {
        Write-Host "==> Installing Rust nightly toolchain..." -ForegroundColor Yellow
        rustup toolchain install nightly
    }
    
    # Set nightly for this directory
    rustup override set nightly
    
    # Build in release mode
    if (-not (Test-Path "target\release\pocx_miner.exe") -or -not (Test-Path "target\release\pocx_plotter.exe")) {
        Write-Host "==> Building PoCX (this may take a few minutes)..." -ForegroundColor Yellow
        cargo build --release
    }
    else {
        Write-Host "==> PoCX binaries already built" -ForegroundColor Gray
    }
    
    Set-Location ..
}

# Build Bitcoin-PoCX (optional - requires more dependencies)
if (Test-Path "bitcoin-pocx") {
    Write-Host "==> Bitcoin-PoCX node found" -ForegroundColor Green
    Write-Host "    Note: Building Bitcoin-PoCX on Windows requires MSVC and dependencies" -ForegroundColor Yellow
    Write-Host "    Refer to bitcoin-pocx/doc/build-windows.md for instructions" -ForegroundColor Yellow
}

Write-Host "==> Dependency build complete!" -ForegroundColor Green

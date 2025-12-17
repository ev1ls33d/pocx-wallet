#!/bin/bash
# Build script for PoCX and Bitcoin-PoCX dependencies

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo "==> Building PoCX dependencies..."

# Build PoCX (Rust)
if [ -d "pocx" ]; then
    echo "==> Building PoCX binaries..."
    cd pocx
    
    # Check if Rust is installed
    if ! command -v cargo &> /dev/null; then
        echo "Error: Rust/Cargo not found. Please install Rust from https://rustup.rs/"
        exit 1
    fi
    
    # Check for nightly toolchain
    if ! rustup toolchain list | grep -q "nightly"; then
        echo "==> Installing Rust nightly toolchain..."
        rustup toolchain install nightly
    fi
    
    # Set nightly for this directory
    rustup override set nightly
    
    # Build in release mode
    if [ ! -f "target/release/pocx_miner" ] || [ ! -f "target/release/pocx_plotter" ]; then
        echo "==> Building PoCX (this may take a few minutes)..."
        cargo build --release
    else
        echo "==> PoCX binaries already built"
    fi
    
    cd ..
fi

# Build Bitcoin-PoCX (optional - requires more dependencies)
if [ -d "bitcoin-pocx" ]; then
    echo "==> Bitcoin-PoCX node found"
    echo "    Note: Building Bitcoin-PoCX requires additional dependencies (autotools, boost, etc.)"
    echo "    Run './build-bitcoin-pocx.sh' separately if needed"
fi

echo "==> Dependency build complete!"

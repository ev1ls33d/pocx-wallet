#!/bin/bash
# Build script for PocxWallet Native Library

set -e

echo "Building PocxWallet Native Library..."

# Create build directory
mkdir -p build
cd build

# Configure with CMake
cmake .. -DCMAKE_BUILD_TYPE=Release

# Build
cmake --build . --config Release -j$(nproc)

echo "Build complete!"
echo "Library location: build/lib/libpocxwallet_native.so"
echo ""
echo "To install system-wide:"
echo "  sudo cmake --install ."
echo ""
echo "To use with .NET application, copy library to:"
echo "  cp lib/libpocxwallet_native.so ../PocxWallet.Cli/bin/Debug/net9.0/"

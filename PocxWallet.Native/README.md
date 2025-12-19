# PocxWallet Native Library

This directory contains the native C++/CUDA wrapper for high-performance vanity address generation.

## Building

### Prerequisites
- CMake 3.18+
- C++17 compiler
- CUDA Toolkit 11+ (optional, for GPU support)

### Linux/macOS
```bash
mkdir build && cd build
cmake ..
make
```

### Windows
```bash
mkdir build && cd build
cmake -G "Visual Studio 17 2022" ..
cmake --build . --config Release
```

## Integration with VanitySearch-PocX

To enable full GPU acceleration:

1. Copy GPU kernels from VanitySearch-PocX:
   - `GPU/GPUEngine.cu`
   - `GPU/GPUHash.h`
   - `GPU/GPUCompute.h`
   - `GPU/GPUGroup.h`
   
2. Update `CMakeLists.txt` to include CUDA sources

3. Implement actual GPU search in `vanitysearch_wrapper.cpp`

## Current Status

This is a scaffold implementation that demonstrates the P/Invoke pattern. The actual CUDA integration requires the VanitySearch-PocX GPU kernels to be integrated.

# GPU Acceleration for Vanity Address Generation

This document describes the GPU acceleration implementation using native CUDA integration via P/Invoke.

## Architecture

### Native Library (C++/CUDA)
Location: `PocxWallet.Native/`

The native library provides a C interface to CUDA-accelerated vanity address search:
- **Header**: `include/vanitysearch_wrapper.h` - C API definitions
- **Implementation**: `src/vanitysearch_wrapper.cpp` - Wrapper implementation
- **Build**: `CMakeLists.txt` - CMake build configuration

### .NET Integration (P/Invoke)
Location: `PocxWallet.Core/VanityAddress/`

- **NativeVanitySearch.cs** - P/Invoke declarations and marshalling
- **GpuVanityAddressGenerator.cs** - High-level generator with auto-fallback

## Building the Native Library

### Prerequisites
- **CMake** 3.18 or higher
- **C++17** compiler (GCC 9+, Clang 10+, MSVC 2019+)
- **CUDA Toolkit** 11+ (optional, for GPU acceleration)

### Linux/macOS
```bash
cd PocxWallet.Native
./build.sh
```

### Windows
```bash
cd PocxWallet.Native
build.bat
```

### Manual Build
```bash
cd PocxWallet.Native
mkdir build && cd build
cmake .. -DCMAKE_BUILD_TYPE=Release
cmake --build . --config Release
```

## Integrating VanitySearch-PocX CUDA Kernels

To achieve 1000x+ speedup, integrate the actual VanitySearch-PocX GPU implementation:

### Step 1: Copy GPU Kernels
```bash
# From VanitySearch-PocX repository
cp -r VanitySearch-PocX/GPU/*.h PocxWallet.Native/include/
cp -r VanitySearch-PocX/GPU/*.cu PocxWallet.Native/src/
```

### Step 2: Update CMakeLists.txt
Add CUDA source files:
```cmake
if(CUDA_FOUND)
    set(CUDA_SOURCES
        src/vanitysearch_wrapper.cpp
        src/GPUEngine.cu
        # Add other .cu files
    )
    add_library(pocxwallet_native SHARED ${CUDA_SOURCES})
endif()
```

### Step 3: Implement GPU Search
Update `vanitysearch_wrapper.cpp` to call actual CUDA kernels:
```cpp
#include "GPUEngine.h"

int vanitysearch_find(...) {
    // Initialize GPU engine
    GPUEngine gpuEngine(...);
    
    // Run CUDA kernels for address generation
    gpuEngine.Launch(...);
    
    // Return results
}
```

## Performance Expectations

### Current (CPU Parallelization)
- ~600 attempts/sec (16 core CPU)
- Uses 256+ parallel workers
- No GPU utilization

### With CUDA Integration
- **100,000+ attempts/sec** (mid-range GPU)
- **1,000,000+ attempts/sec** (high-end GPU)
- Full GPU utilization
- **1000x+ speedup** over standard CPU mode

## Usage

### From .NET Code
```csharp
// Automatically uses native library if available
var generator = new VanityAddressGenerator("madf0x", useGpu: true);
var result = await generator.GenerateAsync(progress, cancellationToken);
```

### Detection
The system automatically detects:
1. Native library availability (`libpocxwallet_native.so` or `pocxwallet_native.dll`)
2. Falls back to CPU parallelization if unavailable
3. Logs which mode is being used

### Library Location
Place the native library in:
- **Linux**: Same directory as executable or `/usr/local/lib`
- **Windows**: Same directory as executable or system PATH
- **macOS**: Same directory as executable or `/usr/local/lib`

## Development Roadmap

### Phase 1: Scaffold (✅ Complete)
- [x] C API header definition
- [x] P/Invoke integration
- [x] Build system (CMake)
- [x] Auto-detection and fallback

### Phase 2: CUDA Integration (Next)
- [ ] Copy VanitySearch-PocX GPU kernels
- [ ] Integrate Secp256k1 GPU implementation
- [ ] Integrate SHA256/RIPEMD160 GPU hashing
- [ ] Integrate Bech32 GPU encoding
- [ ] Implement pattern matching on GPU

### Phase 3: Optimization
- [ ] Multi-GPU support
- [ ] Optimized memory transfers
- [ ] Batch result processing
- [ ] Performance profiling and tuning

### Phase 4: Testing
- [ ] Unit tests for native library
- [ ] Integration tests
- [ ] Performance benchmarks
- [ ] Cross-platform testing

## Troubleshooting

### Library Not Found
```
[GPU Mode] Native library not available, using CPU parallelization
```
**Solution**: Build native library and copy to executable directory

### CUDA Not Available
```
CMake Warning: CUDA not found, building CPU-only version
```
**Solution**: Install CUDA Toolkit or build CPU-only version

### Performance Issues
If native library is present but performance is poor:
1. Verify CUDA kernels are integrated (see Phase 2 above)
2. Check GPU utilization with `nvidia-smi`
3. Verify pattern complexity (longer patterns = harder search)

## Technical Notes

### Why P/Invoke?
- Allows direct integration with CUDA C++
- Zero-copy interop for large data
- Reuses proven VanitySearch-PocX code
- No need to reimplement cryptography in C#/ILGPU

### Memory Management
- Native library manages GPU memory
- .NET only passes strings and receives results
- No memory leaks - proper cleanup in Dispose()

### Thread Safety
- Native library handles internal locking
- .NET wrapper is thread-safe
- Can run multiple searches concurrently

## Contributing

To contribute CUDA kernel integration:
1. Fork repository
2. Integrate VanitySearch-PocX kernels
3. Test on NVIDIA GPU
4. Submit PR with benchmark results

## References

- [VanitySearch-PocX](https://github.com/ev1ls33d/VanitySearch-PocX) - Original CUDA implementation
- [CUDA Programming Guide](https://docs.nvidia.com/cuda/)
- [P/Invoke Documentation](https://docs.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke)

@echo off
REM Build script for PocxWallet Native Library (Windows)

echo Building PocxWallet Native Library...

REM Create build directory
if not exist build mkdir build
cd build

REM Configure with CMake
cmake .. -G "Visual Studio 17 2022" -A x64

REM Build
cmake --build . --config Release

echo Build complete!
echo Library location: build\bin\Release\pocxwallet_native.dll
echo.
echo To use with .NET application, copy library to:
echo   copy bin\Release\pocxwallet_native.dll ..\PocxWallet.Cli\bin\Debug\net9.0\

pause

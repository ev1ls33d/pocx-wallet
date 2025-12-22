# PoCX Wallet - Single Binary Deployment

This document describes how to build and deploy the PoCX Wallet as a single self-contained binary.

## Building Single Binary

The project is configured to build as a single-file executable with an external configuration file.

### Build for Linux (x64)

```bash
cd PocxWallet.Cli
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

### Build for Windows (x64)

```bash
cd PocxWallet.Cli
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

### Build for macOS (x64)

```bash
cd PocxWallet.Cli
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

### Build for macOS (ARM64/Apple Silicon)

```bash
cd PocxWallet.Cli
dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

## Output Files

The build process creates:

1. **pocxwallet** (or **pocxwallet.exe** on Windows) - The single executable binary (~39MB)
2. **appsettings.json** - External configuration file with all settings
3. **\*.pdb** files - Debug symbols (optional, can be deleted)

## Deployment

To deploy the wallet:

1. Copy the `pocxwallet` binary to your target location
2. Copy the `appsettings.json` file to the same directory
3. Make the binary executable (Linux/macOS):
   ```bash
   chmod +x pocxwallet
   ```
4. Run the wallet:
   ```bash
   ./pocxwallet
   ```

## Configuration

All settings are stored in `appsettings.json`. You can modify this file to:

- Change Docker registry and image tags
- Configure service-specific parameters (Bitcoin node, Electrs, Miner, Plotter)
- Set custom paths for wallets, plots, and data directories
- Configure environment variables for services
- Adjust network and container names

### Example Configuration

```json
{
  "UseDocker": true,
  "DockerRegistry": "ghcr.io/ev1ls33d/pocx-wallet",
  "DockerImageTag": "latest",
  "BitcoinNode": {
    "RpcPort": 18332,
    "P2PPort": 18333,
    "AdditionalParams": "-printtoconsole -rpcallowip=0.0.0.0/0 -rpcbind=0.0.0.0",
    "DataDirectory": "./bitcoin-data"
  },
  "Electrs": {
    "HttpPort": 3000,
    "RpcPort": 50001,
    "DataDirectory": "./electrs-data"
  },
  "Miner": {
    "CpuThreads": 8,
    "UseDirectIO": true
  },
  "Plotter": {
    "DefaultWarps": 10,
    "CpuThreads": 4
  }
}
```

## Requirements

### Docker Mode (Recommended)
- Docker or Docker Desktop installed and running
- No additional dependencies required

### Native Mode (Advanced)
If you disable Docker mode in settings:
- Rust nightly toolchain
- Bitcoin-PoCX compiled binaries
- PoCX tools compiled (plotter, miner)

## Size Optimization

To reduce the binary size, you can:

1. Enable trimming (may cause issues with reflection):
   ```bash
   dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o ./publish
   ```

2. Use framework-dependent deployment (requires .NET 9 runtime on target):
   ```bash
   dotnet publish -c Release -r linux-x64 --self-contained false -p:PublishSingleFile=true -o ./publish
   ```

## Security Notes

- The `appsettings.json` file is externalized to allow configuration changes without recompiling
- Keep your wallet files and mnemonics secure
- Never store sensitive data in `appsettings.json`
- Use environment variables for secrets when available

## Troubleshooting

### Binary won't execute
- Linux/macOS: Ensure the binary is executable (`chmod +x pocxwallet`)
- Check that you're using the correct platform binary

### Configuration not loading
- Ensure `appsettings.json` is in the same directory as the binary
- Verify JSON syntax is correct
- Check file permissions

### Docker not available
- Install Docker or enable Docker mode in settings
- On Linux, ensure your user is in the `docker` group
- On Windows, enable WSL2 integration in Docker Desktop

## Updates

To update the wallet:

1. Download or build the new binary
2. Replace the old `pocxwallet` binary
3. Update `appsettings.json` if there are new configuration options
4. Restart the application

Your wallet data, plots, and blockchain data are stored separately and will not be affected.

# PoCX Wallet

A comprehensive .NET 9 HD Wallet implementation for [PoCX (Proof of Capacity X)](https://github.com/PoC-Consortium/pocx) with full node integration, plotting, mining, and transaction capabilities.

## Features

- ✨ **HD Wallet Management**
  - Generate new HD wallets with BIP39 mnemonic seeds (12/15/18/21/24 words)
  - Restore wallets from existing mnemonic phrases
  - BIP32 hierarchical key derivation
  - BIP44-compliant address generation
  - **Proper pocx1q... Bech32 address format** (witness version 0, P2WPKH)
  - Bitcoin-compatible Hash160 (RIPEMD160(SHA256(pubkey))) payload
  - Secure passphrase support

- 🐳 **Docker Container Management**
  - **Docker-first approach** for easy deployment
  - Automated setup and configuration
  - Pre-built Docker images via GitHub Actions
  - One-command service deployment
  - Cross-platform compatibility (Windows WSL2, Linux, macOS)
  - Optional native binary support

- 📊 **Plot Generation**
  - Docker containers or native `pocx_plotter` binary
  - Create plot files for PoCX mining
  - Configurable plot sizes
  - Background service support

- ⛏️ **Mining**
  - Docker containers or native `pocx_miner` binary
  - Easy mining configuration
  - Pool mining support
  - Background service with status monitoring

- 🔗 **Bitcoin-PoCX Node Integration**
  - Docker containers or native `bitcoind` node
  - `bitcoin-cli` integration for RPC commands
  - Background service management
  - Start/stop node from CLI
  - Real-time blockchain synchronization
  - Persistent blockchain data storage

- 🔌 **Electrum Server (Electrs-PoCX)**
  - Docker container support for Electrum server
  - Fast blockchain indexing with RocksDB
  - HTTP REST API for blockchain queries
  - Electrum RPC protocol support
  - Enables lightweight wallet access without a full node
  - Efficient transaction and address lookups

- 💰 **Transaction Features**
  - Check wallet balance via node RPC
  - Send funds to other addresses
  - View transaction history
  - Integrated with Bitcoin-PoCX node

- 🎯 **Vanity Address Generator**
  - Generate pocx1q... addresses with custom patterns
  - Multi-threaded CPU implementation
  - Pattern matching on Bech32 encoded addresses
  - GPU acceleration support (planned)

- 💻 **CLI Interface**
  - Beautiful console UI with Spectre.Console
  - Interactive menu system
  - Keyboard navigation
  - Background service monitoring
  - Configuration management
  - Cross-platform compatible (Windows, Linux, macOS)

## Prerequisites

### Required
- .NET 9.0 SDK or later
- Docker (recommended) or Rust nightly toolchain for native binaries
- Linux, macOS, or Windows (with WSL2 for Docker on Windows)

### Docker Setup (Recommended)
The wallet uses Docker containers by default for easy setup:
- **Linux**: Docker Engine
- **Windows**: Docker Desktop with WSL2
- **macOS**: Docker Desktop

See [DOCKER.md](DOCKER.md) for detailed Docker setup instructions.

## Quick Start

### Using Docker (Recommended)

1. **Clone the Repository**
   ```bash
   git clone https://github.com/ev1ls33d/pocx-wallet.git
   cd pocx-wallet
   ```

2. **Build the Wallet**
   ```bash
   dotnet build
   ```

3. **Run the Wallet**
   ```bash
   cd PocxWallet.Cli
   dotnet run
   ```

4. **Setup Docker** (First Time)
   - Navigate to: `Settings → Setup Docker`
   - The wallet will guide you through Docker installation
   - Pull the pre-built images: `Settings → Pull Docker Images`

5. **Start Services**
   - Bitcoin Node: `Bitcoin-PoCX Node → Start Node`
   - Mining: `Mining → Start Mining`
   - Plotting: `Plotting → Create Plot`

All services automatically use Docker containers. See [DOCKER.md](DOCKER.md) for more details.

### Using Native Binaries (Advanced)

If you prefer to build and run native binaries instead of Docker:

1. **Clone with Submodules**
   ```bash
   git clone --recursive https://github.com/ev1ls33d/pocx-wallet.git
   cd pocx-wallet
   ```

2. **Build Dependencies**
   ```bash
   # Linux/Mac
   ./build-dependencies.sh
   
   # Windows
   ./build-dependencies.ps1
   ```

3. **Build the Wallet**
   ```bash
   dotnet build
   ```

4. **Disable Docker Mode**
   ```bash
   cd PocxWallet.Cli
   dotnet run
   ```
   Navigate to: `Settings → Toggle Docker Mode` to disable Docker and use native binaries.

### Demo Mode

For a quick demonstration without setup:

```bash
cd PocxWallet.Cli
dotnet run -- --demo
```

## Project Structure

```
pocx-wallet/
├── PocxWallet.Core/          # Core wallet and cryptography
│   ├── Wallet/               # HD wallet implementation
│   └── VanityAddress/        # Vanity address generator
├── PocxWallet.Protocol/      # PoCX binary wrappers
│   └── Wrappers/             # Process wrappers for binaries
├── PocxWallet.Mining/        # Mining services (future)
├── PocxWallet.Cli/           # CLI application
│   ├── Commands/             # Command implementations
│   └── Configuration/        # App settings
├── pocx/                     # PoCX submodule (Rust)
├── bitcoin-pocx/             # Bitcoin-PoCX node submodule
└── PocxWallet.sln            # Solution file
```

## PoCX Address Format

This wallet generates **proper Bech32 addresses** following the PoCX specification:

- **Format**: `pocx1q...` (lowercase)
- **Encoding**: Bech32 (BIP-173)
- **Witness Version**: 0 (P2WPKH)
- **Payload**: 20 bytes (Hash160 = RIPEMD160(SHA256(public key)))
- **Example**: `pocx1q20kypkuljzp8ulujfw67ryuwnh5h2d2ygadj23`

These addresses are:
- Bitcoin-compatible in structure
- Human-readable with error detection
- Lowercase for better readability
- Checksum-protected against typos

## Usage

### Creating a New Wallet

1. Launch the application: `dotnet run`
2. Select "💰 Wallet Management"
3. Choose "Create New Wallet"
4. Select mnemonic word count (12/15/18/21/24 words)
5. Optionally add a passphrase for extra security
6. **IMPORTANT**: Save your mnemonic phrase securely!

### Restoring a Wallet

1. Select "💰 Wallet Management"
2. Choose "Restore Wallet from Mnemonic"
3. Enter your mnemonic phrase
4. Enter passphrase if you used one
5. Wallet restored!

### Starting Bitcoin-PoCX Node

1. Select "🔗 Bitcoin-PoCX Node"
2. Choose "Start Node"
3. Enter data directory (or press Enter for default)
4. Node starts in Docker container automatically
5. Monitor status with "Show Node Status"

**Note**: First-time sync may take several hours depending on blockchain size.

### Creating Plot Files

1. Select "📊 Plotting"
2. Choose "Create Plot"
3. Enter your account ID (from wallet)
4. Specify plot directory (default: `./plots`)
5. Set number of warps (1 warp ≈ 1GB)
6. Plotter runs in background (Docker container)
7. Check progress in background services

### Mining PoCX

1. Create a miner configuration:
   - Select "⛏️ Mining"
   - Choose "Create Miner Config"
   - Enter pool details and account ID
2. Start mining:
   - Choose "Start Mining"
   - Miner runs in Docker container
   - Monitor with "Show Mining Status"
3. Stop mining when done

### Generating Vanity Addresses

1. Select "✨ Vanity Address Generator"
2. Enter the pattern you want in the address
3. Wait for a matching address to be generated
4. Save the mnemonic phrase securely!

**Note**: Vanity generation can take time depending on pattern complexity.

## Configuration

The application uses `appsettings.json` for configuration:

```json
{
  "PoCXBinariesPath": "./pocx/target/release",
  "PlotDirectory": "./plots",
  "WalletFilePath": "./wallet.json",
  "MinerConfigPath": "./config.yaml",
  "UseDocker": true,
  "DockerRegistry": "ghcr.io/ev1ls33d/pocx-wallet",
  "DockerImageTag": "latest"
}
```

**Key Settings**:
- `UseDocker`: Enable/disable Docker mode (default: `true`)
- `DockerRegistry`: Docker registry for images
- `DockerImageTag`: Image version to use
- Native binary paths used when Docker mode is disabled

You can modify these settings through the CLI "⚙️ Settings" menu or by editing the file directly.

## Service Management

### Docker Mode (Default)

Services run in isolated Docker containers:

- **bitcoin-pocx**: Bitcoin-PoCX full node (bitcoind, bitcoin-cli)
- **pocx-miner**: PoCX mining service
- **pocx-plotter**: Plot file generator

Benefits:
- ✅ No manual compilation required
- ✅ Consistent environment across platforms
- ✅ Easy updates via image pulls
- ✅ Resource isolation and management
- ✅ Automatic dependency handling

### Native Mode (Optional)

For advanced users who prefer native binaries:

The wallet can wrap local PoCX binaries:
- **pocx_plotter**: Generate plot files
- **pocx_miner**: Mine PoCX blocks
- **pocx_verifier**: Verify plot file integrity
- **bitcoind**: Bitcoin-PoCX full node

These must be built from the `pocx` and `bitcoin-pocx` submodules.

## Security Considerations

- ⚠️ **Never share your mnemonic phrase** with anyone
- 🔒 Store your mnemonic phrase in a secure location (offline preferred)
- 🛡️ Use a passphrase for additional security
- 💾 Keep multiple backups of your mnemonic in different secure locations
- 🚫 Never store your mnemonic in plain text on your computer

## Development

### Building

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Code Structure

- **PocxWallet.Core**: Core cryptography and wallet functionality using NBitcoin
- **PocxWallet.Protocol**: Wrappers for PoCX Rust binaries
- **PocxWallet.Cli**: Interactive command-line interface using Spectre.Console

## Dependencies

### Core Libraries
- [NBitcoin](https://github.com/MetacoSA/NBitcoin) - Bitcoin library for .NET
- [Spectre.Console](https://spectreconsole.net/) - Beautiful console applications

### External Services
- [Docker](https://www.docker.com/) - Container platform (recommended)
- [PoCX](https://github.com/PoC-Consortium/pocx) - Proof of Capacity X framework (branch: master)
- [Bitcoin-PoCX](https://github.com/PoC-Consortium/bitcoin) - Bitcoin node with PoCX support (branch: pocx-v30-RC2)

### Docker Images
Pre-built images available at:
- `ghcr.io/ev1ls33d/pocx-wallet/bitcoin-pocx:latest` - Bitcoin-PoCX node
- `ghcr.io/ev1ls33d/pocx-wallet/pocx:latest` - PoCX tools (miner, plotter)

## Roadmap

### Completed ✅
- [x] HD wallet with BIP39/BIP32/BIP44
- [x] PoCX binary wrappers (plotter, miner)
- [x] CLI interface with Spectre.Console
- [x] Vanity address generation (CPU)
- [x] Bitcoin-PoCX node wrapper and integration
- [x] Background service management
- [x] Transaction signing and broadcasting
- [x] Balance checking via node RPC
- [x] Transaction history
- [x] Cross-platform compatibility
- [x] **Docker container orchestration**
- [x] **Automated Docker image builds via GitHub Actions**
- [x] **Docker-first service deployment**

### In Progress 🚧
- [ ] GPU-accelerated vanity address generation (OpenCL/CUDA)
- [ ] Avalonia cross-platform GUI

### Planned 📋
- [ ] Address book
- [ ] QR code generation
- [ ] Hardware wallet support
- [ ] Multi-signature support
- [ ] Docker Compose configuration
- [ ] Kubernetes deployment manifests

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [PoC-Consortium](https://github.com/PoC-Consortium) for PoCX and Bitcoin-PoCX
- [NBitcoin](https://github.com/MetacoSA/NBitcoin) for Bitcoin cryptography
- [Spectre.Console](https://spectreconsole.net/) for beautiful CLI

## Support

For issues, questions, or contributions, please visit the [GitHub repository](https://github.com/ev1ls33d/pocx-wallet).

---

**Disclaimer**: This is experimental software. Use at your own risk. Always test with small amounts first.
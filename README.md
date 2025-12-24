# PoCX Wallet

A comprehensive CLI HD Wallet implementation for [PoCX](https://github.com/PoC-Consortium/pocx) with full node integration, plotting, mining, and transaction capabilities.

> **🚀 BETA Release** - This is an early release of PoCX Wallet. Please report any issues on [GitHub](https://github.com/ev1ls33d/pocx-wallet/issues).

## Features

### 💰 HD Wallet Management
- Generate new HD wallets with BIP39 mnemonic seeds (12-word by default)
- Restore wallets from existing mnemonic phrases
- BIP84 derivation path for native SegWit (P2WPKH)
- **Proper `pocx1q...` Bech32 address format** (witness version 0, P2WPKH)
- **Testnet support with `tpocx1q...` addresses**
- Bitcoin-compatible Hash160 (RIPEMD160(SHA256(pubkey))) payload
- Secure optional passphrase support
- Multi-wallet support with easy switching
- Automatic wallet persistence to `wallet.json`

### 🎯 Vanity Address Generator
- Generate `pocx1q...` or `tpocx1q...` addresses with custom patterns
- Multi-threaded CPU implementation (uses all available cores)
- Pattern matching on Bech32 encoded addresses
- Valid characters: `qpzry9x8gf2tvdw0s3jn54khce6mua7l` (Bech32 charset)

### 🐳 Docker Container Management
- **Docker-first approach** for easy deployment
- Dynamic service configuration via `services.yaml`
- Pre-built Docker images via GitHub Actions
- Per-service parameter configuration
- Cross-platform compatibility (Windows WSL2, Linux, macOS)

### 🔗 Bitcoin-PoCX Node Integration
- Full Bitcoin-PoCX node running in Docker
- Automatic wallet import with descriptor support
- `bitcoin-cli` commands executed directly from the CLI
- Real-time blockchain synchronization
- Configurable network parameters (testnet by default)

### 🔌 Electrs Server (Electrum Protocol)
- Electrum protocol server for lightweight wallet access
- Fast blockchain indexing with RocksDB
- HTTP REST API for blockchain queries

### ⛏️ Mining & Plotting
- **PoCX Plotter**: Generate plot files for Proof-of-Capacity mining
- **PoCX Miner**: Mine with your plot files
- **PoCX Aggregator**: Aggregate multiple miners
- GPU passthrough support for OpenCL acceleration

### 💸 Transaction Features
- Check wallet balance via node RPC
- Send funds to other addresses
- View transaction history
- Create and sign transactions
- PSBT (Partially Signed Bitcoin Transaction) support

## Quick Start

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download) or later
- [Docker](https://www.docker.com/products/docker-desktop) (recommended)

### Installation

```bash
# Clone the repository
git clone https://github.com/ev1ls33d/pocx-wallet.git
cd pocx-wallet

# Build the wallet
dotnet build

# Run the wallet
cd PocxWallet.Cli
dotnet run
```

### First Steps

1. **Create a Wallet**: Navigate to `[Wallet] → Manage → Create → Random Address`
2. **Save Your Mnemonic**: Securely store the displayed 12-word mnemonic phrase
3. **Start the Node**: Select `[Node]` from the main menu and start the service
4. **Import to Node**: Import your wallet to the node for balance checking and transactions

## Project Structure

```
pocx-wallet/
├── PocxWallet.Core/           # Core wallet and cryptography
│   ├── Address/               # Bech32 encoding
│   ├── Wallet/                # HD wallet implementation
│   └── VanityAddress/         # Vanity address generator
├── PocxWallet.Cli/            # CLI application
│   ├── Commands/              # Command implementations
│   ├── Configuration/         # Service definitions and wallet manager
│   ├── Services/              # Docker and background service managers
│   └── Resources/             # UI strings and localization
├── services.yaml              # Service configuration (node, miner, plotter, etc.)
├── bitcoin/                   # Bitcoin-PoCX submodule
├── electrs/                   # Electrs-PoCX submodule
├── pocx/                      # PoCX tools submodule
└── PocxWallet.sln            # Solution file
```

## PoCX Address Format

| Property | Value |
|----------|-------|
| **Format** | `pocx1q...` (mainnet) / `tpocx1q...` (testnet) |
| **Encoding** | Bech32 (BIP-173) |
| **Witness Version** | 0 (P2WPKH) |
| **Payload** | 20 bytes (Hash160) |
| **Example** | `pocx1q20kypkuljzp8ulujfw67ryuwnh5h2d2ygadj23` |

## CLI Menu Structure

```
Main Menu
├── [Wallet] Wallet Management
│   ├── Manage
│   │   ├── Create → Random Address / Vanity Address
│   │   ├── Import → To Node / From Mnemonic
│   │   └── Remove
│   ├── Select (switch active wallet)
│   ├── Info (balance, addresses, blockchain info)
│   ├── Transaction (send, sign, broadcast)
│   └── Settings (auto-save, startup wallet, auto-import)
├── [Node] Bitcoin-PoCX Node
│   ├── Start/Stop Node
│   ├── View Logs
│   ├── Parameters (network, RPC, performance)
│   └── Settings (ports, volumes, container)
├── [Plot] PoCX Plotter
├── [Mine] PoCX Miner
├── [Aggregator] PoCX Aggregator
├── [Electrs] Electrs Server
└── [Exit]
```

## Configuration

### services.yaml

All Docker services are configured in `services.yaml`. This file defines:
- Container images and repositories
- Port mappings
- Volume mounts
- CLI parameters
- Menu structure

Example parameter configuration:
```yaml
parameters:
  - name: "testnet"
    cli_flag: "-testnet"
    type: "bool"
    default: true
    value: true
    description: "Use the test network (testnet3)"
```

### wallet.json

Wallet data is stored in `wallet.json`:
```json
{
  "version": "1.0",
  "active_wallet": "default",
  "wallets": [
    {
      "name": "default",
      "mnemonic": "...",
      "mainnet_address": "pocx1q...",
      "testnet_address": "tpocx1q..."
    }
  ],
  "settings": {
    "auto_save": false,
    "auto_import_to_node": false
  }
}
```

## Docker Services

| Service | Image | Description |
|---------|-------|-------------|
| `bitcoin-node` | `ghcr.io/ev1ls33d/pocx-wallet/bitcoin:latest` | Full Bitcoin-PoCX node |
| `electrs` | `ghcr.io/ev1ls33d/pocx-wallet/electrs:latest` | Electrum server |
| `plotter` | `ghcr.io/ev1ls33d/pocx-wallet/pocx:latest` | Plot file generator |
| `miner` | `ghcr.io/ev1ls33d/pocx-wallet/pocx:latest` | Mining client |
| `aggregator` | `ghcr.io/ev1ls33d/pocx-wallet/pocx:latest` | Mining aggregator |

## Security

> ⚠️ **IMPORTANT SECURITY NOTES**

- **Never share your mnemonic phrase** with anyone
- Store your mnemonic phrase offline in a secure location
- Use a passphrase for additional security
- Keep multiple backups in different secure locations
- Wallet files contain sensitive data - protect them accordingly

## Development

```bash
# Build
dotnet build

# Run tests
dotnet test

# Run with verbose output
cd PocxWallet.Cli
dotnet run
```

### Dependencies

| Library | Purpose |
|---------|---------|
| [NBitcoin](https://github.com/MetacoSA/NBitcoin) | Bitcoin cryptography |
| [Spectre.Console](https://spectreconsole.net/) | CLI interface |
| [YamlDotNet](https://github.com/aaubry/YamlDotNet) | YAML parsing |

## Documentation

📖 See the [Wiki](wiki/) for detailed documentation:
- [Home](wiki/Home.md) - Overview
- [Installation](wiki/Installation.md) - Setup guide
- [Wallet Management](wiki/Wallet-Management.md) - Wallet features
- [Services](wiki/Services.md) - Docker services
- [Configuration](wiki/Configuration.md) - Configuration reference
- [CLI Reference](wiki/CLI-Reference.md) - All commands
- [Architecture](wiki/Architecture.md) - Technical details

## Roadmap

### ✅ Completed
- HD wallet with BIP39/BIP84
- Multi-wallet support
- Vanity address generation (CPU)
- Bitcoin-PoCX node integration
- Docker container orchestration
- Transaction support via node RPC
- Dynamic service configuration (services.yaml)

### 🚧 In Progress
- GPU-accelerated vanity generation (OpenCL/CUDA)

### 📋 Planned
- Cross-platform GUI (Avalonia)
- Hardware wallet support
- Multi-signature support
- Docker Compose configuration

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [PoC-Consortium](https://github.com/PoC-Consortium) for PoCX and Bitcoin-PoCX
- [NBitcoin](https://github.com/MetacoSA/NBitcoin) for Bitcoin cryptography
- [Spectre.Console](https://spectreconsole.net/) for beautiful CLI

---

**⚠️ Disclaimer**: This is experimental BETA software. Use at your own risk. Always test with testnet first before using mainnet.

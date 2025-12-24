Welcome to the **PoCX Wallet** documentation. This wiki provides comprehensive documentation for all features, services, and configuration options.

## Overview

PoCX Wallet is a command-line HD (Hierarchical Deterministic) wallet for the PoCX cryptocurrency. It combines wallet management, blockchain node operations, and mining capabilities in a single tool.

### Key Features

| Feature | Description |
|---------|-------------|
| **HD Wallet** | BIP39/BIP84 compliant wallet with mnemonic backup |
| **Multi-Wallet** | Manage multiple wallets with easy switching |
| **Vanity Addresses** | Generate custom `pocx1q...` addresses |
| **Node Integration** | Full Bitcoin-PoCX node in Docker |
| **Mining Support** | Plot generation and mining via Docker |
| **Electrum Server** | Lightweight wallet access via Electrs |

## Quick Navigation

### Getting Started
- [Installation Guide](Installation.md) - Prerequisites and setup instructions
- [Quick Start](#quick-start) - Get running in 5 minutes

### Features
- [Wallet Management](Wallet-Management.md) - Create, import, and manage wallets
- [Services](Services.md) - Docker service management
- [CLI Reference](CLI-Reference.md) - Complete menu and command reference

### Configuration
- [Configuration Guide](Configuration.md) - services.yaml and wallet.json reference
- [Architecture](Architecture.md) - Technical documentation

## Quick Start

### 1. Install Prerequisites
```bash
# Install .NET 9.0 SDK
# https://dotnet.microsoft.com/download

# Install Docker
# https://www.docker.com/products/docker-desktop
```

### 2. Clone and Build
```bash
git clone https://github.com/ev1ls33d/pocx-wallet.git
cd pocx-wallet
dotnet build
```

### 3. Run the Wallet
```bash
cd PocxWallet.Cli
dotnet run
```

### 4. Create Your First Wallet
1. Select `[Wallet]` from the main menu
2. Go to `Manage → Create → Random Address`
3. **Save your 12-word mnemonic phrase securely!**
4. The wallet is automatically saved to `wallet.json`

### 5. Start the Node (Optional)
1. Select `[Node]` from the main menu
2. Choose `Start Node`
3. Wait for the container to start
4. Import your wallet to the node for balance checking

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                      PoCX Wallet CLI                        │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │   Wallet    │  │   Service   │  │   Docker Service    │  │
│  │  Commands   │  │  Menu Bld   │  │     Manager         │  │
│  └─────────────┘  └─────────────┘  └─────────────────────┘  │
├─────────────────────────────────────────────────────────────┤
│                     PoCX Wallet Core                        │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │  HD Wallet  │  │   Bech32    │  │  Vanity Generator   │  │
│  │   (NBit)    │  │   Encoder   │  │   (Multi-thread)    │  │
│  └─────────────┘  └─────────────┘  └─────────────────────┘  │
├─────────────────────────────────────────────────────────────┤
│                    Docker Containers                        │
│  ┌───────────┐ ┌───────────┐ ┌───────────┐ ┌───────────┐   │
│  │  Bitcoin  │ │  Electrs  │ │  Plotter  │ │   Miner   │   │
│  │   Node    │ │  Server   │ │           │ │           │   │
│  └───────────┘ └───────────┘ └───────────┘ └───────────┘   │
└─────────────────────────────────────────────────────────────┘
```

## Address Format

PoCX uses Bech32 addresses with the `pocx` prefix:

| Network | Prefix | Example |
|---------|--------|---------|
| Mainnet | `pocx1q` | `pocx1q20kypkuljzp8ulujfw67ryuwnh5h2d2ygadj23` |
| Testnet | `tpocx1q` | `tpocx1qhs8v5n8t8u7kvp8t4tqvxrfmh3...` |

The `1q` indicates witness version 0 (native SegWit, P2WPKH).

## Security Best Practices

> **▲ Critical Security Information**

1. **Mnemonic Phrase**: This is the master key to all your funds
   - Write it down on paper (not digital)
   - Store in multiple secure locations
   - Never share with anyone
   - Never enter on websites or apps you don't trust

2. **Passphrase** (Optional): Adds an extra layer of security
   - Creates a completely different wallet if changed
   - No way to recover if forgotten

3. **Wallet File**: `wallet.json` contains sensitive data
   - Set appropriate file permissions
   - Consider encryption at rest
   - Back up regularly

## Support

- **GitHub Issues**: [Report bugs or request features](https://github.com/ev1ls33d/pocx-wallet/issues)
- **Documentation**: This wiki
- **Source Code**: [GitHub Repository](https://github.com/ev1ls33d/pocx-wallet)

---

*PoCX Wallet is experimental software. Use at your own risk.*
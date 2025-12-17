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

- 📊 **Plot Generation**
  - Wrapper for `pocx_plotter` binary
  - Create plot files for PoCX mining
  - Configurable plot sizes
  - Background service support

- ⛏️ **Mining**
  - Wrapper for `pocx_miner` binary
  - Easy mining configuration
  - Pool mining support
  - Background service with status monitoring

- 🔗 **Bitcoin-PoCX Node Integration**
  - Full `bitcoind` node wrapper
  - `bitcoin-cli` integration for RPC commands
  - Background service management
  - Start/stop node from CLI
  - Real-time blockchain synchronization

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

- .NET 9.0 SDK or later
- Rust nightly toolchain (for building PoCX binaries)
- Linux, macOS, or Windows

## Quick Start

### 1. Clone the Repository

```bash
git clone --recursive https://github.com/ev1ls33d/pocx-wallet.git
cd pocx-wallet
```

### 2. Build Dependencies

Run the automated build script to build PoCX binaries:

```bash
# Linux/Mac
./build-dependencies.sh

# Windows
./build-dependencies.ps1
```

This will:
- Check for and install Rust nightly if needed
- Build `pocx_plotter` and `pocx_miner` binaries
- Prepare the environment for the wallet

### 3. Build the Wallet

```bash
dotnet build
```

### 4. Run the Wallet

#### CLI Application

The wallet runs in interactive mode by default:

```bash
cd PocxWallet.Cli
dotnet run
```

Or run the compiled binary directly:

```bash
./bin/Debug/net9.0/pocxwallet
```

For demo mode:

```bash
dotnet run -- --demo
# or
./bin/Debug/net9.0/pocxwallet --demo
```

#### GUI Application (Avalonia)

Run the cross-platform desktop application:

```bash
cd PocxWallet.UI/PocxWallet.UI.Desktop
dotnet run
```

The GUI provides a modern, user-friendly interface with:
- Dashboard with balance and network status
- Wallet management (create/restore)
- Send and receive transactions
- Transaction history
- Multi-account support
- Contact management
- Settings and configuration

See [PocxWallet.UI/README.md](PocxWallet.UI/README.md) for more details on the UI.

## Project Structure

```
pocx-wallet/
├── PocxWallet.Core/          # Core wallet and cryptography
│   ├── Wallet/               # HD wallet implementation
│   ├── VanityAddress/        # Vanity address generator
│   └── Services/             # Service interfaces (API layer)
├── PocxWallet.Protocol/      # PoCX binary wrappers
│   └── Wrappers/             # Process wrappers for binaries
├── PocxWallet.Mining/        # Mining services (future)
├── PocxWallet.Cli/           # CLI application
│   ├── Commands/             # Command implementations
│   └── Configuration/        # App settings
├── PocxWallet.UI/            # Cross-platform GUI (Avalonia)
│   ├── PocxWallet.UI/        # Shared UI library
│   ├── Desktop/              # Desktop application
│   ├── Android/              # Android application
│   ├── iOS/                  # iOS application
│   └── Browser/              # WebAssembly browser app
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

### Creating Plot Files

1. Select "📊 Plotting"
2. Choose "Create Plot"
3. Enter your account ID (from wallet)
4. Specify plot directory
5. Set number of warps (1 warp ≈ 1GB)
6. Wait for plotting to complete

### Mining PoCX

1. Create a miner configuration:
   - Select "⛏️ Mining"
   - Choose "Create Miner Config"
   - Enter pool details and account ID
2. Start mining:
   - Choose "Start Mining"
   - Monitor output in real-time
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
  "MinerConfigPath": "./config.yaml"
}
```

You can modify these settings through the CLI "⚙️ Settings" menu or by editing the file directly.

## PoCX Binary Wrappers

The wallet wraps the following PoCX binaries:

- **pocx_plotter**: Generate plot files
- **pocx_miner**: Mine PoCX blocks
- **pocx_verifier**: Verify plot file integrity
- **pocx_address**: Address utilities

These binaries must be built from the `pocx` submodule.

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

- [NBitcoin](https://github.com/MetacoSA/NBitcoin) - Bitcoin library for .NET
- [Spectre.Console](https://spectreconsole.net/) - Beautiful console applications
- [PoCX](https://github.com/PoC-Consortium/pocx) - Proof of Capacity X framework (branch: master)
- [Bitcoin-PoCX](https://github.com/PoC-Consortium/bitcoin) - Bitcoin node with PoCX support (branch: pocx-v30-RC2)

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
- [x] Avalonia UI foundation with ReactiveUI
- [x] Service layer interfaces (API architecture)

### In Progress 🚧
- [ ] Avalonia GUI implementation (views created, needs service integration)
- [ ] Service layer implementation
- [ ] Multi-account management
- [ ] Contact/address book
- [ ] QR code generation and scanning

### Planned 📋
- [ ] GPU-accelerated vanity address generation (OpenCL/CUDA)
- [ ] Encrypted messaging with transactions
- [ ] Multi-output transactions
- [ ] Alias system
- [ ] Deep linking support
- [ ] Internationalization (25+ languages)
- [ ] Hardware wallet support
- [ ] Multi-signature support
- [ ] Mobile optimization (Android/iOS)
- [ ] Electrs integration for lightweight clients

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
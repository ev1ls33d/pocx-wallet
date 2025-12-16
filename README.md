# PoCX Wallet

A comprehensive .NET 9 HD Wallet implementation for [PoCX (Proof of Capacity X)](https://github.com/PoC-Consortium/pocx) with plotting, mining, and vanity address generation capabilities.

## Features

- ✨ **HD Wallet Management**
  - Generate new HD wallets with BIP39 mnemonic seeds (12/15/18/21/24 words)
  - Restore wallets from existing mnemonic phrases
  - BIP32 hierarchical key derivation
  - BIP44-compliant address generation
  - Secure passphrase support

- 📊 **Plot Generation**
  - Wrapper for `pocx_plotter` binary
  - Create plot files for PoCX mining
  - Configurable plot sizes
  - Plot verification support

- ⛏️ **Mining**
  - Wrapper for `pocx_miner` binary
  - Easy mining configuration
  - Pool mining support
  - Real-time mining status

- 🎯 **Vanity Address Generator**
  - Generate addresses with custom patterns
  - Multi-threaded CPU implementation
  - GPU acceleration support (planned)

- 💻 **CLI Interface**
  - Beautiful console UI with Spectre.Console
  - Interactive menu system
  - Easy-to-use commands
  - Configuration management

- 🔗 **Bitcoin-PoCX Node Integration**
  - Submodule integration with bitcoin-pocx node
  - Full node support (ready for wrapper implementation)

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

### 2. Build PoCX Binaries

```bash
cd pocx
rustup toolchain install nightly
rustup override set nightly
cargo build --release
cd ..
```

### 3. Build the Wallet

```bash
dotnet build
```

### 4. Run the Wallet

```bash
cd PocxWallet.Cli
dotnet run
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
- [PoCX](https://github.com/PoC-Consortium/pocx) - Proof of Capacity X framework
- [Bitcoin-PoCX](https://github.com/PoC-Consortium/bitcoin) - Bitcoin node with PoCX support

## Roadmap

- [x] HD wallet with BIP39/BIP32/BIP44
- [x] PoCX binary wrappers (plotter, miner, verifier)
- [x] CLI interface with Spectre.Console
- [x] Vanity address generation (CPU)
- [ ] GPU-accelerated vanity address generation (OpenCL/CUDA)
- [ ] Bitcoin-PoCX node wrapper and integration
- [ ] Avalonia cross-platform GUI
- [ ] Transaction signing and broadcasting
- [ ] Address book
- [ ] QR code generation
- [ ] Hardware wallet support

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
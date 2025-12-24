# Architecture

Technical documentation of PoCX Wallet's internal architecture.

## Overview

PoCX Wallet is built with a modular architecture separating core cryptographic functionality from the CLI interface and service management.

```
┌─────────────────────────────────────────────────────────────────┐
│                         PocxWallet.Cli                          │
│  ┌─────────────┐ ┌─────────────────┐ ┌───────────────────────┐  │
│  │   Program   │ │  WalletCommands │ │ DynamicServiceMenu    │  │
│  │  (Entry)    │ │                 │ │     Builder           │  │
│  └─────────────┘ └─────────────────┘ └───────────────────────┘  │
│  ┌─────────────┐ ┌─────────────────┐ ┌───────────────────────┐  │
│  │   Wallet    │ │ ServiceDefiniti │ │  DockerService        │  │
│  │  Manager    │ │    onLoader     │ │    Manager            │  │
│  └─────────────┘ └─────────────────┘ └───────────────────────┘  │
├─────────────────────────────────────────────────────────────────┤
│                        PocxWallet.Core                          │
│  ┌─────────────────┐ ┌─────────────────┐ ┌───────────────────┐  │
│  │     HDWallet    │ │  Bech32Encoder  │ │ VanityAddress     │  │
│  │    (NBitcoin)   │ │                 │ │   Generator       │  │
│  └─────────────────┘ └─────────────────┘ └───────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

## Project Structure

### PocxWallet.Core

Core cryptographic and wallet functionality.

```
PocxWallet.Core/
├── Address/
│   └── Bech32Encoder.cs      # Custom Bech32 encoding for pocx prefix
├── Wallet/
│   └── HDWallet.cs           # HD wallet implementation using NBitcoin
└── VanityAddress/
    └── VanityAddressGenerator.cs  # Multi-threaded vanity search
```

#### HDWallet.cs

The main wallet class providing:
- Mnemonic generation (BIP39)
- Key derivation (BIP84 paths)
- Address generation (Bech32 with pocx prefix)
- WIF export
- Descriptor generation with checksums

**Key Methods:**
```csharp
// Create new wallet
public static HDWallet CreateNew(WordCount wordCount, string? passphrase)

// Restore from mnemonic
public static HDWallet FromMnemonic(string mnemonic, string? passphrase)

// Derive PoCX address
public string GetPoCXAddress(uint account, uint index, bool testnet, bool change)

// Get descriptor with checksum
public string GetDescriptor(bool testnet, uint account, uint index)
```

#### Bech32Encoder.cs

Custom Bech32 encoding supporting:
- `pocx` prefix for mainnet
- `tpocx` prefix for testnet
- Witness version 0 (P2WPKH)

#### VanityAddressGenerator.cs

Multi-threaded vanity address search:
- Uses all available CPU cores
- Progress reporting via IProgress<long>
- Cancellation support
- Pattern matching on generated addresses

### PocxWallet.Cli

Command-line interface and service management.

```
PocxWallet.Cli/
├── Program.cs                    # Entry point and main menu
├── Commands/
│   └── WalletCommands.cs         # All wallet menu handlers
├── Configuration/
│   ├── ServiceDefinition.cs      # YAML model classes
│   ├── ServiceDefinitionLoader.cs # YAML loading/saving
│   ├── DynamicServiceMenuBuilder.cs # Dynamic menu generation
│   ├── WalletManager.cs          # Multi-wallet persistence
│   └── CommandTemplateEngine.cs  # Custom command processing
├── Services/
│   ├── DockerServiceManager.cs   # Docker container operations
│   └── BackgroundServiceManager.cs # Background process tracking
└── Resources/
    └── Strings.cs                # UI strings for localization
```

#### Program.cs

Application entry point:
- Initializes services
- Loads `services.yaml`
- Runs main menu loop
- Handles graceful shutdown

#### WalletCommands.cs

Handles all wallet-related menus:
- Create (random, vanity)
- Import (from mnemonic, to node)
- Select/Switch
- Info queries
- Transaction operations
- Settings

#### DynamicServiceMenuBuilder.cs

Builds menus dynamically from `services.yaml`:
- Reads service definitions
- Generates menu structure
- Handles service actions (start/stop/logs)
- Parameter and settings management

#### WalletManager.cs

Singleton managing multiple wallets:
- Load/save to `wallet.json`
- Active wallet tracking
- Legacy format migration
- Settings persistence

#### DockerServiceManager.cs

Docker container management:
- Container lifecycle (create, start, stop, remove)
- Log retrieval
- Command execution in containers
- Network management

## Data Flow

### Wallet Creation Flow

```
User Input → WalletCommands.CreateRandomWalletAsync()
           → HDWallet.CreateNew()
           → NBitcoin.Mnemonic (generates entropy)
           → HDWallet.GetPoCXAddress()
           → Bech32Encoder.Encode()
           → WalletManager.AddWallet()
           → WalletManager.Save() → wallet.json
```

### Service Start Flow

```
User Selection → DynamicServiceMenuBuilder.StartServiceAsync()
              → BuildCommand() (from parameters)
              → BuildVolumeMappings()
              → BuildPortMappings()
              → DockerServiceManager.StartContainerAsync()
              → docker run command
```

### Node Command Flow

```
User Action → WalletCommands.ExecuteNodeCommandAsync()
           → Check node running status
           → DockerServiceManager.ExecInContainerAsync()
           → docker exec command
           → Parse and display output
```

## Key Design Decisions

### 1. Dynamic Service Configuration

Services are defined in `services.yaml` rather than hardcoded:
- Easy to add new services without code changes
- Parameter validation from schema
- Portable configuration

### 2. Multi-Wallet Support

The wallet file supports multiple named wallets:
- One active wallet at a time
- Automatic legacy format migration
- Settings stored alongside wallets

### 3. BIP84 Derivation

Uses BIP84 (native SegWit) paths:
- `m/84'/0'/0'/0/0` for mainnet
- `m/84'/1'/0'/0/0` for testnet
- Compatible with modern Bitcoin wallets

### 4. Spectre.Console UI

Rich terminal UI using Spectre.Console:
- Selection prompts for menus
- Progress bars for long operations
- Panels for structured output
- Markup for colors

### 5. Docker-First Architecture

Default deployment via Docker:
- No build dependencies on host
- Consistent environment
- Easy updates via image pulls

## Dependencies

### Core Dependencies

| Package | Purpose |
|---------|---------|
| NBitcoin | Bitcoin cryptography |
| YamlDotNet | YAML parsing |
| Spectre.Console | Terminal UI |

### External Services

| Service | Purpose |
|---------|---------|
| Docker | Container runtime |
| Bitcoin-PoCX Node | Blockchain operations |
| Electrs | Electrum protocol |

## Security Architecture

### Mnemonic Handling

- Generated using NBitcoin's secure random
- Stored encrypted in wallet.json (user responsibility)
- Never transmitted over network
- Displayed once during creation

### Private Key Derivation

- Keys derived on-demand from mnemonic
- Never stored separately
- WIF export for node import only

### Container Isolation

- Services run in Docker containers
- Network isolation via Docker network
- Volume mounts for persistent data only

## Extension Points

### Adding New Services

1. Add service definition to `services.yaml`
2. Define parameters, ports, volumes
3. Service automatically appears in menu

### Custom Actions

Services can define custom commands:
```yaml
command:
  binary: "bitcoin-cli"
  arguments: ["getblockchaininfo"]
  show_output: true
```

### Localization

All UI strings centralized in `Strings.cs`:
- Can be extended for multiple languages
- Consistent formatting

---

[← CLI Reference](CLI-Reference.md) | [Home](Home.md)

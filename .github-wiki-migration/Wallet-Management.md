# Wallet Management

This guide covers all wallet-related features in PoCX Wallet.

## Overview

PoCX Wallet supports multiple wallets with automatic persistence. Wallets use BIP84 derivation paths for native SegWit addresses.

### Key Concepts

| Term | Description |
|------|-------------|
| **Mnemonic** | 12-word seed phrase that generates all keys |
| **Passphrase** | Optional extra word for additional security |
| **HD Wallet** | Hierarchical Deterministic - one seed generates unlimited addresses |
| **BIP84** | Derivation path standard for native SegWit (P2WPKH) |

## Wallet Menu Structure

```
[Wallet] Wallet Management
├── Manage
│   ├── Create
│   │   ├── Random Address    # Generate new wallet
│   │   └── Vanity Address    # Generate with pattern
│   ├── Import
│   │   ├── To Node           # Import existing wallet to Bitcoin node
│   │   └── From Mnemonic     # Restore from seed phrase
│   └── Remove                # Delete a wallet
├── Select                    # Switch active wallet
├── Info                      # Balance, addresses, blockchain info
├── Transaction               # Send funds, PSBT operations
└── Settings                  # Auto-save, startup wallet, auto-import
```

## Creating Wallets

### Random Address (Recommended)

Creates a new wallet with a randomly generated 12-word mnemonic.

1. Navigate to `[Wallet] → Manage → Create → Random Address`
2. Enter an optional passphrase (or leave empty)
3. View your wallet details:
   - **Mnemonic phrase** (12 words)
   - **Mainnet address** (`pocx1q...`)
   - **Testnet address** (`tpocx1q...`)
   - **WIF private keys** (mainnet and testnet)
   - **Descriptors** (for node import)
4. Save wallet when prompted
5. Enter a unique wallet name

> **▲ CRITICAL**: Write down your mnemonic phrase and store it securely! This is the only way to recover your funds if you lose access to the wallet.

### Vanity Address

Generate a wallet with a custom pattern in the address.

1. Navigate to `[Wallet] → Manage → Create → Vanity Address`
2. Enter your desired pattern (e.g., `pocx` for an address containing "pocx")
3. Choose mainnet or testnet
4. Enter an optional passphrase
5. Wait while the generator searches (shows attempt count)
6. Save when found

**Valid Characters**: `qpzry9x8gf2tvdw0s3jn54khce6mua7l`

> ⚡ **Tip**: Shorter patterns are found faster. A 3-character pattern typically takes seconds, while 5+ characters can take hours.

## Importing Wallets

### From Mnemonic

Restore a wallet from an existing seed phrase.

1. Navigate to `[Wallet] → Manage → Import → From Mnemonic`
2. Enter your 12-word (or 24-word) mnemonic phrase
3. Enter the passphrase if you used one originally
4. View the restored wallet details
5. Save and name the wallet

### To Node

Import an existing wallet to the Bitcoin-PoCX node for balance checking and transactions.

1. Navigate to `[Wallet] → Manage → Import → To Node`
2. Select the wallet to import
3. If the node isn't running, you'll be asked to start it
4. The wallet creates a descriptor wallet on the node
5. Your descriptor is imported automatically

## Selecting Wallets

Switch between multiple wallets:

1. Navigate to `[Wallet] → Select`
2. View all wallets with addresses
3. Active wallet marked with green dot `●`
4. Select a wallet to make it active

## Wallet Info

Query information about your active wallet via the Bitcoin node.

### Available Commands

| Command | Description |
|---------|-------------|
| **Check Balance** | Get wallet balance from node |
| **Show Addresses** | List all addresses with received amounts |
| **List Unspent** | Show unspent transaction outputs (UTXOs) |
| **Get Wallet Info** | Detailed wallet statistics |
| **Get Blockchain Info** | Current blockchain sync status |
| **Transaction History** | Last 10 transactions |

> **Note**: These commands require the Bitcoin node to be running and the wallet imported.

## Transactions

Send funds and manage transactions via the node.

### Send Funds

1. Navigate to `[Wallet] → Transaction → Send Funds`
2. Enter destination address
3. Enter amount in BTC
4. Confirm execution on node
5. Transaction is broadcast

### Create PSBT

Create a Partially Signed Bitcoin Transaction for offline signing.

1. Navigate to `[Wallet] → Transaction → Create PSBT`
2. Enter destination address
3. Enter amount
4. PSBT is created and displayed

### Sign Transaction

Sign a raw transaction with your wallet keys.

### Broadcast Transaction

Send a signed transaction to the network.

### Decode PSBT

View the details of a PSBT string.

## Wallet Settings

Configure wallet behavior:

| Setting | Description | Default |
|---------|-------------|---------|
| **Auto-Save Wallets** | Automatically save new wallets | Off |
| **Startup Wallet** | Wallet to load on startup | None |
| **Auto-Import to Node** | Automatically import new wallets to node | Off |

Access via `[Wallet] → Settings`

## Removing Wallets

1. Navigate to `[Wallet] → Manage → Remove`
2. Select wallet to remove
3. Confirm deletion
4. Optionally unload from Bitcoin node

> **▲ Warning**: Ensure you have the mnemonic backed up before removing a wallet!

## Technical Details

### Derivation Path

PoCX Wallet uses BIP84 derivation:

```
m/84'/coin'/account'/chain/index
```

| Component | Value |
|-----------|-------|
| Purpose | `84'` (BIP84 - native SegWit) |
| Coin Type | `0'` (mainnet) or `1'` (testnet) |
| Account | `0'` (default) |
| Chain | `0` (external) or `1` (change) |
| Index | `0` (first address) |

Full mainnet path: `m/84'/0'/0'/0/0`  
Full testnet path: `m/84'/1'/0'/0/0`

### Address Generation

1. Derive private key from mnemonic + derivation path
2. Calculate public key (compressed, 33 bytes)
3. Hash160: `RIPEMD160(SHA256(pubkey))` → 20 bytes
4. Encode as Bech32 with `pocx` prefix and witness version 0

### Descriptor Format

Wallets are imported to the node using descriptors:

```
wpkh(KwDiBf89QgGbjEhKnhXJuH7LrciVrZi3qYjgd9M7rFU73sVHnoWn)#descriptor_checksum
```

## wallet.json File Format

Wallets are stored in `wallet.json`:

```json
{
  "version": "1.0",
  "active_wallet": "main",
  "wallets": [
    {
      "name": "main",
      "mnemonic": "word1 word2 ... word12",
      "passphrase": "",
      "mainnet_address": "pocx1q...",
      "testnet_address": "tpocx1q...",
      "created": "2024-01-15T12:00:00Z",
      "pattern": null
    }
  ],
  "settings": {
    "default_wallet_path": "./wallet.json",
    "auto_save": false,
    "startup_wallet": null,
    "auto_import_to_node": false
  }
}
```

> **▲ Security**: This file contains your mnemonic phrases. Protect it accordingly!

---

[← Back to Home](Home.md) | [Services →](Services.md)

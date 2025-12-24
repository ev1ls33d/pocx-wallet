# CLI Reference

Complete reference for all menu options and commands in PoCX Wallet.

## Main Menu

```
Main Menu
────────────────────────────────────────
> [Wallet]         Wallet Management
  [Node]           Bitcoin-PoCX Node      ●
  [Plot]           PoCX Plotter           ●
  [Mine]           PoCX Miner             ●
  [Aggregator]     PoCX Aggregator        ●
  [Electrs]        Electrs Server         ●
  [Exit]           Exit
```

### Status Indicators
- 🟢 (`●` green) - Service running
- 🔴 (`●` red) - Service stopped

## Wallet Menu

### Main Options

| Option | Description |
|--------|-------------|
| **Manage** | Create, import, remove wallets |
| **Select** | Switch active wallet |
| **Info** | Query wallet/blockchain info |
| **Transaction** | Send funds, PSBT operations |
| **Settings** | Configure wallet behavior |

### Manage Submenu

#### Create
| Option | Description |
|--------|-------------|
| **Random Address** | Generate new wallet with random 12-word mnemonic |
| **Vanity Address** | Generate wallet with custom address pattern |

#### Import
| Option | Description |
|--------|-------------|
| **To Node** | Import existing wallet to Bitcoin node |
| **From Mnemonic** | Restore wallet from seed phrase |

#### Remove
Select a wallet to remove from the wallet file.

### Select Submenu

Lists all wallets with:
- Name (padded)
- Full mainnet address
- Active indicator (🟢 for active wallet)

### Info Submenu

| Option | Node Command | Description |
|--------|--------------|-------------|
| **Check Balance** | `getbalance` | Get wallet balance |
| **Show Addresses** | `listreceivedbyaddress 0 true` | List addresses |
| **List Unspent** | `listunspent` | Show UTXOs |
| **Get Wallet Info** | `getwalletinfo` | Wallet statistics |
| **Get Blockchain Info** | `getblockchaininfo` | Blockchain status |
| **Transaction History** | `listtransactions "*" 10` | Recent transactions |

> **Note**: Requires node running and wallet imported.

### Transaction Submenu

| Option | Description |
|--------|-------------|
| **Send Funds** | Send BTC to an address |
| **Create Transaction** | Create raw transaction (shows template) |
| **Sign Transaction** | Sign raw transaction with wallet |
| **Broadcast Transaction** | Send signed transaction to network |
| **Create PSBT** | Create Partially Signed Bitcoin Transaction |
| **Decode PSBT** | Decode and display PSBT contents |

### Settings Submenu

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| Auto-Save Wallets | bool | Off | Auto-save new wallets |
| Startup Wallet | string | None | Wallet to load on startup |
| Auto-Import to Node | bool | Off | Auto-import new wallets |

## Service Menus

All services (Node, Plotter, Miner, Aggregator, Electrs) share a common menu structure.

### Common Options

| Option | Description |
|--------|-------------|
| **Start/Stop** | Toggle service state |
| **View Logs** | Display last 50 log lines |
| **Parameters** | Configure CLI parameters |
| **Settings** | Configure container settings |

### Parameters Menu

Shows currently set parameters with values.

| Action | Description |
|--------|-------------|
| **[[Add Parameter]]** | Add a new parameter from available list |
| **Select parameter** | Edit or remove the parameter |

#### Editing Parameters

| Type | Edit Options |
|------|--------------|
| `bool` | Toggle Value |
| `int`, `string`, `string[]` | Edit Value |
| All types | Remove Parameter |

### Settings Menu

| Setting | Description |
|---------|-------------|
| **Repository** | Docker image repository |
| **Tag** | Image version tag |
| **Container Name** | Docker container name |
| **Network** | Docker network |
| **Volumes** | Volume mount configuration |
| **Ports** | Port mapping configuration |
| **Environment** | Environment variables |

## Bitcoin Node Parameters

Complete parameter reference for the Bitcoin-PoCX node.

### Network Selection

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `testnet` | bool | true | Use testnet |
| `regtest` | bool | false | Regression test mode |
| `signet` | bool | false | Use signet |

### PoCX-Specific

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `miningserver` | bool | true | Enable mining RPC |

### Connection

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `addnode` | string[] | [] | Nodes to connect to |
| `connect` | string[] | [] | Connect only to these |
| `listen` | bool | true | Accept connections |
| `maxconnections` | int | 125 | Max connections |
| `port` | int | 18333 | P2P port |
| `proxy` | string | "" | SOCKS5 proxy |
| `dns` | bool | true | Allow DNS lookups |
| `dnsseed` | bool | true | Use DNS seeds |

### RPC

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `rpcport` | int | 18332 | RPC port |
| `rpcbind` | string | 0.0.0.0 | RPC bind address |
| `rpcallowip` | string[] | [0.0.0.0/0] | Allowed IPs |
| `rpcuser` | string | "" | RPC username |
| `rpcpassword` | string | "" | RPC password |
| `rpcthreads` | int | 4 | RPC threads |

### Wallet

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `disablewallet` | bool | false | Disable wallet |
| `wallet` | string | "" | Wallet to load |
| `walletbroadcast` | bool | true | Broadcast transactions |

### Indexing

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `txindex` | bool | true | Transaction index |
| `prune` | int | 0 | Prune blocks (0=disable) |
| `reindex` | bool | false | Rebuild index |

### Performance

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `dbcache` | int | 450 | DB cache (MB) |
| `par` | int | 0 | Verification threads |
| `maxmempool` | int | 300 | Mempool size (MB) |

### Debug

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `printtoconsole` | bool | true | Print to console |
| `debug` | string | "" | Debug categories |
| `logips` | bool | false | Log IP addresses |
| `logtimestamps` | bool | true | Log timestamps |

### ZeroMQ

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `zmqpubrawblock` | string | "" | Block publish endpoint |
| `zmqpubrawtx` | string | "" | TX publish endpoint |
| `zmqpubhashblock` | string | "" | Block hash endpoint |
| `zmqpubhashtx` | string | "" | TX hash endpoint |

## Plotter Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `address` | string | **Required** | Your PoCX address |
| `warps` | int | 976 | Warps to plot (1=1GiB) |
| `number` | int | 0 | Number of plots (0=fill) |
| `path` | string[] | [/plots] | Output paths |
| `compression` | int | 1 | Compression (1-6) |
| `cpu` | int | 0 | CPU threads (0=auto) |
| `gpu` | string[] | [] | GPU config |
| `memory` | string | 0B | Memory limit |
| `quiet` | bool | false | Minimal output |
| `benchmark` | bool | false | Benchmark mode |

## Keyboard Navigation

| Key | Action |
|-----|--------|
| ↑ / ↓ | Navigate menu |
| Enter | Select option |
| Esc | (Not implemented - use Back option) |

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Normal exit |
| 1 | Error |

---

[← Configuration](Configuration.md) | [Architecture →](Architecture.md)

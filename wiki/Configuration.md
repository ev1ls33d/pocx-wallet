# Configuration Reference

PoCX Wallet uses two main configuration files: `services.yaml` for Docker services and `wallet.json` for wallet data.

## services.yaml

The primary configuration file defining all Docker services and their parameters.

### File Structure

```yaml
version: "1.0"

defaults:
  docker_network: "pocx"
  restart_policy: "unless-stopped"
  log_driver: "json-file"
  log_max_size: "10m"
  log_max_files: 3

services:
  - id: "bitcoin-node"
    name: "Bitcoin-PoCX Node"
    # ... service definition

categories:
  - id: "infrastructure"
    name: "Infrastructure"
    # ... category definition

parameter_categories:
  - id: "network"
    name: "Network"
    # ... parameter category definition
```

### Global Defaults

| Setting | Default | Description |
|---------|---------|-------------|
| `docker_network` | `pocx` | Docker network for all services |
| `restart_policy` | `unless-stopped` | Container restart policy |
| `log_driver` | `json-file` | Docker logging driver |
| `log_max_size` | `10m` | Maximum log file size |
| `log_max_files` | `3` | Number of log files to keep |

### Service Definition

Each service has the following structure:

```yaml
- id: "service-id"
  name: "Service Display Name"
  description: "Description of the service"
  category: "infrastructure"
  menu_label: "[Label]"
  documentation_url: "https://..."
  enabled: true
  
  container:
    image: "image-name"
    repository: "ghcr.io/ev1ls33d/pocx-wallet"
    default_tag: "latest"
    container_name_default: "pocx-service"
    working_dir: "/root"
    binary: "executable"
  
  source:
    repository: "https://github.com/..."
    branch: "master"
    build_command: "cargo build --release"
    binary_paths:
      - "target/release/binary"
  
  ports:
    - name: "rpc"
      container_port: 18332
      host_port_default: 18332
      description: "RPC port"
      protocol: "tcp"
      optional: false
  
  volumes:
    - name: "data"
      host_path_default: "./data"
      container_path: "/data"
      read_only: false
      description: "Data directory"
      is_file: false
  
  environment:
    - name: "TZ"
      value: "Europe/Berlin"
      description: "Timezone"
  
  parameters:
    - name: "testnet"
      cli_flag: "-testnet"
      type: "bool"
      default: true
      value: true
      description: "Use testnet"
      category: "network"
  
  depends_on:
    - service_id: "bitcoin-node"
      condition: "healthy"
      reason: "Requires synced node"
  
  health_check:
    command: "bitcoin-cli getblockchaininfo"
    interval_seconds: 30
    timeout_seconds: 10
    retries: 3
    start_period_seconds: 60
  
  menu:
    main_menu_order: 1
    submenu:
      - action: "toggle"
        label_running: "Stop Node"
        label_stopped: "Start Node"
      - action: "logs"
        label: "View Logs"
      - action: "parameters"
        label: "Parameters"
      - action: "settings"
        label: "Settings"
  
  settings:
    - key: "Repository"
      setting_path: "BitcoinNode.Repository"
      type: "string"
      category: "container"
```

### Parameter Types

| Type | Description | Example |
|------|-------------|---------|
| `bool` | Boolean flag | `true`, `false` |
| `int` | Integer value | `18332` |
| `string` | Text value | `"0.0.0.0"` |
| `string[]` | Array of strings | `["value1", "value2"]` |

### Parameter Properties

| Property | Type | Description |
|----------|------|-------------|
| `name` | string | Parameter identifier |
| `cli_flag` | string | CLI flag (e.g., `-testnet`) |
| `cli_alias` | string | Alternative flag (e.g., `--testnet`) |
| `type` | string | Data type |
| `default` | any | Default value |
| `value` | any | User-set value (passed to container) |
| `description` | string | Help text |
| `category` | string | Parameter category |
| `required` | bool | Whether required |
| `sensitive` | bool | Hide value in UI |
| `enum` | string[] | Valid values for selection |
| `validation.min` | int | Minimum value (for int) |
| `validation.max` | int | Maximum value (for int) |
| `use_equals` | bool | Use `=` syntax (`-flag=value`) |
| `hidden` | bool | Hide from UI |

### User Overrides

Users can override settings at runtime. Overrides are stored in services.yaml:

```yaml
# Port override
ports:
  - name: "rpc"
    container_port: 18332
    host_port_default: 18332
    host_port_override: 19332  # User override

# Volume override
volumes:
  - name: "data"
    host_path_default: "./data"
    host_path_override: "/mnt/blockchain"  # User override

# Container name override
container_name_override: "my-custom-node"

# Network override
network_override: "my-network"
```

## wallet.json

Stores wallet data and settings.

### File Structure

```json
{
  "version": "1.0",
  "active_wallet": "main",
  "wallets": [
    {
      "name": "main",
      "mnemonic": "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about",
      "passphrase": "",
      "mainnet_address": "pocx1q...",
      "testnet_address": "tpocx1q...",
      "created": "2024-01-15T12:00:00.000Z",
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

### Wallet Entry Properties

| Property | Type | Description |
|----------|------|-------------|
| `name` | string | Unique wallet name |
| `mnemonic` | string | 12/24-word seed phrase |
| `passphrase` | string | Optional BIP39 passphrase |
| `mainnet_address` | string | Primary mainnet address |
| `testnet_address` | string | Primary testnet address |
| `created` | string | ISO 8601 creation timestamp |
| `pattern` | string? | Vanity pattern (if generated) |

### Settings Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `default_wallet_path` | string | `./wallet.json` | Wallet file location |
| `auto_save` | bool | `false` | Auto-save new wallets |
| `startup_wallet` | string? | `null` | Wallet to load on startup |
| `auto_import_to_node` | bool | `false` | Auto-import to Bitcoin node |

## Environment Variables

The CLI uses environment variables for certain configurations:

| Variable | Description |
|----------|-------------|
| `POCX_WALLET_PATH` | Override default wallet.json path |
| `POCX_SERVICES_PATH` | Override default services.yaml path |

## File Locations

| File | Default Location | Description |
|------|------------------|-------------|
| `services.yaml` | Repository root | Service definitions |
| `wallet.json` | `./wallet.json` | Wallet data |
| Blockchain data | `./bitcoin-pocx/` | Node blockchain data |
| Electrs data | `./electrs-data/` | Electrs index |
| Plot files | `./plots/` | Mining plot files |
| Miner config | `./miner_config.yaml` | Miner configuration |
| Aggregator config | `./aggregator_config.yaml` | Aggregator configuration |

## Security Considerations

### Sensitive Data

The following files contain sensitive data:

| File | Sensitive Data |
|------|----------------|
| `wallet.json` | Mnemonic phrases |
| `services.yaml` | RPC passwords (if set) |
| `miner_config.yaml` | Pool credentials |

**Recommendations:**
- Set restrictive file permissions (`chmod 600`)
- Don't commit to version control
- Back up securely
- Consider full-disk encryption

### Example .gitignore

```gitignore
# Wallet data
wallet.json

# Blockchain data
bitcoin-pocx/
electrs-data/
plots/

# Configs with credentials
miner_config.yaml
aggregator_config.yaml

# Log files
*.log
```

---

[← Services](Services.md) | [CLI Reference →](CLI-Reference.md)

# Docker Services

PoCX Wallet manages multiple Docker services for blockchain operations. All services are defined in `services.yaml`.

## Service Overview

| Service | Container | Purpose | Default Ports |
|---------|-----------|---------|---------------|
| **Bitcoin-PoCX Node** | `pocx-node` | Full blockchain node | 18332 (RPC), 18333 (P2P) |
| **Electrs Server** | `pocx-electrs` | Electrum protocol server | 3000 (HTTP), 50001 (RPC) |
| **PoCX Plotter** | `pocx-plotter` | Generate plot files | - |
| **PoCX Miner** | `pocx-miner` | Mine with plot files | - |
| **PoCX Aggregator** | `pocx-aggregator` | Aggregate multiple miners | 8080 (API) |

## Service Menu Structure

Each service has a consistent submenu:

```
[Service Name]
├── Start/Stop Service    # Toggle service state
├── View Logs            # Show last 50 log lines
├── Parameters           # Configure CLI parameters
└── Settings             # Configure container settings
```

## Bitcoin-PoCX Node

The full Bitcoin node with PoCX consensus support.

### Quick Start
1. Select `[Node]` from main menu
2. Choose `Start Node`
3. Wait for container to start
4. Node will begin syncing the blockchain

### Key Parameters

| Parameter | CLI Flag | Type | Default | Description |
|-----------|----------|------|---------|-------------|
| `testnet` | `-testnet` | bool | true | Use testnet |
| `miningserver` | `-miningserver` | bool | true | Enable mining RPC |
| `txindex` | `-txindex` | bool | true | Full transaction index |
| `rpcbind` | `-rpcbind` | string | `0.0.0.0` | RPC bind address |
| `rpcallowip` | `-rpcallowip` | string[] | `0.0.0.0/0` | Allowed RPC IPs |
| `printtoconsole` | `-printtoconsole` | bool | true | Print logs to console |
| `dbcache` | `-dbcache` | int | 450 | Database cache (MB) |

### Volumes

| Volume | Container Path | Description |
|--------|---------------|-------------|
| `data` | `/root/.bitcoin-pocx` | Blockchain data and wallet files |

### Ports

| Port | Protocol | Description |
|------|----------|-------------|
| 18332 | TCP | JSON-RPC API |
| 18333 | TCP | P2P network |
| 28332 | TCP | ZeroMQ blocks (optional) |
| 28333 | TCP | ZeroMQ transactions (optional) |

### Common Operations

**Check sync status:**
```bash
docker exec pocx-node bitcoin-cli -testnet getblockchaininfo
```

**Get balance:**
```bash
docker exec pocx-node bitcoin-cli -testnet -rpcwallet=mywallet getbalance
```

## Electrs Server

Electrum protocol server for lightweight wallet access.

### Dependencies
- Requires Bitcoin-PoCX Node to be running and synced

### Key Parameters

| Parameter | CLI Flag | Default | Description |
|-----------|----------|---------|-------------|
| `network` | `--network` | `testnet` | Network type |
| `db_dir` | `--db-dir` | `/data` | Database directory |
| `daemon_rpc_addr` | `--daemon-rpc-addr` | `pocx-node:18332` | Bitcoin node RPC |
| `http_addr` | `--http-addr` | `0.0.0.0:3000` | HTTP API address |
| `electrum_rpc_addr` | `--electrum-rpc-addr` | `0.0.0.0:50001` | Electrum RPC |

### Volumes

| Volume | Container Path | Description |
|--------|---------------|-------------|
| `data` | `/data` | Electrs index database |
| `bitcoin-pocx` | `/root/.bitcoin-pocx` | Node data (read-only) |

### Ports

| Port | Protocol | Description |
|------|----------|-------------|
| 3000 | TCP | HTTP REST API |
| 50001 | TCP | Electrum RPC (mainnet) |
| 60001 | TCP | Electrum RPC (testnet) |
| 4224 | TCP | Prometheus metrics (optional) |

## PoCX Plotter

Generate plot files for Proof-of-Capacity mining.

### Key Parameters

| Parameter | CLI Flag | Type | Default | Description |
|-----------|----------|------|---------|-------------|
| `address` | `-i` | string | **Required** | Your PoCX address |
| `warps` | `-w` | int | 976 | Number of warps (1 warp = 1 GiB) |
| `number` | `-n` | int | 0 | Number of plots (0 = fill disk) |
| `path` | `-p` | string[] | `/plots` | Output directory |
| `compression` | `-x` | int | 1 | Compression level (1-6) |
| `cpu` | `-c` | int | 0 | CPU threads (0 = auto) |
| `gpu` | `-g` | string[] | - | GPU config (OpenCL) |

### Volumes

| Volume | Container Path | Description |
|--------|---------------|-------------|
| `plots` | `/plots` | Output directory for plot files |

### GPU Passthrough

When `-g` (gpu) parameter is set, the container runs with `--gpus all` for OpenCL access.

**Example GPU config:**
```
platform_id:device_id:cores
0:0:1024
```

## PoCX Miner

Mine PoCX using your plot files.

### Dependencies
- Requires plot files
- Requires Bitcoin-PoCX Node or pool connection

### Configuration File

The miner uses a YAML configuration file:

```yaml
chains:
  - name: "pool"
    base_url: "http://pool.example.com:8080"
    api_path: "/pocx"
    accounts:
      - account: "pocx1q..."

plot_dirs:
  - "/plots"

get_mining_info_interval: 1000
timeout: 5000
hdd_use_direct_io: true
cpu_threads: 0
show_progress: true
console_log_level: "Info"
```

### Key Parameters

| Parameter | CLI Flag | Default | Description |
|-----------|----------|---------|-------------|
| `config` | `-c` | `/config/config.yaml` | Config file path |

### Volumes

| Volume | Container Path | Description |
|--------|---------------|-------------|
| `plots` | `/plots` | Plot files (read-only) |
| `config` | `/config/miner_config.yaml` | Configuration file |

## PoCX Aggregator

Aggregate multiple miners to a single upstream pool or wallet.

### Key Parameters (via config file)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `listen_address` | string | `0.0.0.0:8080` | API listen address |
| `upstream.url` | string | **Required** | Pool/wallet URL |
| `upstream.submission_mode` | string | `pool` | `pool` or `wallet` |
| `block_time_secs` | int | 120 | Expected block time |
| `dashboard.enabled` | bool | true | Enable web dashboard |
| `dashboard.listen_address` | string | `0.0.0.0:8081` | Dashboard address |

### Ports

| Port | Protocol | Description |
|------|----------|-------------|
| 8080 | TCP | Mining API |
| 8081 | TCP | Web dashboard |

## Managing Services

### Starting a Service

1. Navigate to the service menu (e.g., `[Node]`)
2. Select `Start Node` (or equivalent)
3. The container will be created and started
4. Status indicator changes from 🔴 to 🟢

### Stopping a Service

1. Navigate to the service menu
2. Select `Stop Node` (or equivalent)
3. Container is stopped gracefully

### Viewing Logs

1. Navigate to service menu
2. Select `View Logs`
3. Shows last 50 log lines with status

### Configuring Parameters

1. Navigate to service menu
2. Select `Parameters`
3. View currently set parameters
4. Use `[[Add Parameter]]` to add new ones
5. Select a parameter to edit or remove

### Configuring Settings

Container-level settings like repository, tag, ports, volumes:

1. Navigate to service menu
2. Select `Settings`
3. Modify as needed:
   - Repository
   - Tag (version)
   - Container Name
   - Network
   - Volumes
   - Ports
   - Environment Variables

## Docker Network

All services run on the `pocx` Docker network for internal communication.

```bash
# View network
docker network inspect pocx

# Services can reference each other by container name
# e.g., electrs connects to pocx-node:18332
```

## Troubleshooting

### Container Won't Start

1. Check logs: `docker logs pocx-<service>`
2. Verify image exists: `docker images | grep pocx`
3. Check port conflicts: `docker ps`
4. Ensure volumes exist and have proper permissions

### Service Not Responding

1. Check container status: `docker ps -a | grep pocx`
2. View recent logs: Service menu → View Logs
3. Restart the service

### Out of Disk Space

1. Prune unused Docker data: `docker system prune`
2. Check blockchain data size
3. Consider using pruning for the node

---

[← Wallet Management](Wallet-Management.md) | [Configuration →](Configuration.md)

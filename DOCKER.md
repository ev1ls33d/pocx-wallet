# Docker Container Management

PoCX Wallet supports running services in Docker containers for easier deployment and management. This guide explains how to use Docker with the PoCX Wallet.

## Overview

The wallet can orchestrate the following Docker containers:
- **bitcoin-pocx-node**: Bitcoin-PoCX full node (bitcoind, bitcoin-cli)
- **pocx-miner**: PoCX mining service
- **pocx-plotter**: PoCX plot file generator
- **electrs-pocx**: Electrum server for lightweight wallet access and blockchain indexing

## Benefits of Using Docker

- ✅ **Easy Setup**: No need to manually build PoCX binaries
- ✅ **Isolation**: Services run in isolated containers
- ✅ **Portability**: Works consistently across Windows, Linux, and macOS
- ✅ **Resource Management**: Better control over CPU and memory usage
- ✅ **Updates**: Pull new images to update services

## Prerequisites

### Linux

Docker must be installed and running:

```bash
# Install Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Start Docker service
sudo systemctl start docker
sudo systemctl enable docker

# Add your user to docker group (optional, avoids sudo)
sudo usermod -aG docker $USER
# Log out and back in for this to take effect
```

### Windows

1. Install [Docker Desktop](https://www.docker.com/products/docker-desktop)
2. Enable WSL2 integration in Docker Desktop settings
3. Ensure Docker Desktop is running

### macOS

1. Install [Docker Desktop](https://www.docker.com/products/docker-desktop)
2. Start Docker Desktop application

## Quick Start

### 1. Setup Docker (First Time Only)

Launch the wallet and go to:
```
Main Menu → Docker Container Management → Setup Docker
```

The wallet will:
- Check if Docker is installed
- Provide installation instructions if needed
- (Linux only) Optionally install Docker automatically

### 2. Pull Docker Images

Before starting containers, pull the pre-built images:
```
Main Menu → Docker Container Management → Pull Docker Images
```

This downloads:
- `ghcr.io/ev1ls33d/pocx-wallet/bitcoin-pocx:latest`
- `ghcr.io/ev1ls33d/pocx-wallet/pocx:latest`
- `ghcr.io/ev1ls33d/pocx-wallet/electrs-pocx:latest`

### 3. Start Services

#### Bitcoin-PoCX Node

```
Main Menu → Docker Container Management → Start Bitcoin-PoCX Node (Container)
```

You'll be asked for:
- Data directory on host (default: `./bitcoin-data`)

The node will:
- Mount your data directory to persist blockchain data
- Expose RPC port 18332 for wallet communication
- Expose P2P port 18333 for network communication

#### PoCX Miner

```
Main Menu → Docker Container Management → Start Miner (Container)
```

You'll be asked for:
- Plot directory on host (default: `./plots`)
- Config file must exist at `./config.yaml`

The miner will:
- Mount your plot directory (read-only access)
- Mount your config directory
- Start mining using the configuration

#### PoCX Plotter

```
Main Menu → Docker Container Management → Start Plotter (Container)
```

You'll be asked for:
- Account ID
- Plot directory on host
- Number of warps (1 warp ≈ 1GB)

The plotter will:
- Mount your plot directory (write access)
- Generate plot files in the background
- Automatically exit when plotting is complete

#### Electrs-PoCX Server

```
Main Menu → Docker Container Management → Start Electrs-PoCX Server (Container)
```

You'll be asked for:
- Electrs data directory on host (default: `./electrs-data`)
- Bitcoin-PoCX data directory (default: `./bitcoin-data`)

The Electrs server will:
- Connect to your Bitcoin-PoCX node to index the blockchain
- Mount the electrs data directory to store the index
- Expose HTTP API on port 3000 for REST queries
- Expose Electrum RPC on port 50001 for Electrum wallet connections
- Provide fast blockchain queries without requiring a full node

**Note:** Electrs requires a synced Bitcoin-PoCX node and significant disk space for the index (approximately 600GB+ after compaction).

## Managing Containers

### Check Container Status

```
Main Menu → Docker Container Management → Check Docker Status
```

Shows:
- Docker installation status
- Running containers
- Container status and ports

### View Container Logs

```
Main Menu → Docker Container Management → View Container Logs
```

Select a container and specify number of log lines to view.

### Stop Containers

Individual stop commands:
```
Main Menu → Docker Container Management → Stop Bitcoin-PoCX Node (Container)
Main Menu → Docker Container Management → Stop Miner (Container)
Main Menu → Docker Container Management → Stop Electrs-PoCX Server (Container)
```

Or stop all containers from the background services view.

### Remove Containers

To completely remove all PoCX containers:
```
Main Menu → Docker Container Management → Remove All Containers
```

This stops and deletes containers, but preserves your data and plots.

## Docker Images

### Pre-built Images

Images are automatically built via GitHub Actions and published to:
- https://github.com/ev1ls33d/pocx-wallet/pkgs/container/bitcoin-pocx
- https://github.com/ev1ls33d/pocx-wallet/pkgs/container/pocx
- https://github.com/ev1ls33d/pocx-wallet/pkgs/container/electrs-pocx

### Building Images Locally

If you want to build images yourself:

#### Bitcoin-PoCX Image

```bash
docker build -t bitcoin-pocx:local -f Dockerfile.bitcoin-pocx .
```

#### PoCX Tools Image

```bash
docker build -t pocx:local -f Dockerfile.pocx .
```

#### Electrs-PoCX Image

```bash
docker build -t electrs-pocx:local -f Dockerfile.electrs-pocx .
```

To use local images, update settings:
```
Main Menu → Settings → Change Docker Registry
# Enter: local (without the image name)
```

## Configuration

### Docker Settings

Available in `appsettings.json`:

```json
{
  "UseDocker": true,
  "DockerRegistry": "ghcr.io/ev1ls33d/pocx-wallet",
  "DockerImageTag": "latest",
  "BitcoinContainerName": "bitcoin-pocx-node",
  "MinerContainerName": "pocx-miner",
  "PlotterContainerName": "pocx-plotter",
  "ElectrsContainerName": "electrs-pocx"
}
```

### Toggle Docker Mode

You can switch between Docker and native binaries:
```
Main Menu → Settings → Toggle Docker Mode
```

When Docker mode is disabled, the wallet uses native binaries from:
- `PoCXBinariesPath` (default: `./pocx/target/release`)
- `BitcoinBinariesPath` (default: `./bitcoin-pocx/src`)

## Volume Mounts

Containers automatically mount directories for data persistence:

| Container | Host Path | Container Path | Purpose |
|-----------|-----------|----------------|---------|
| bitcoin-pocx-node | `./bitcoin-data` | `/root/.bitcoin` | Blockchain data |
| pocx-miner | `./plots` | `/plots` | Plot files (read) |
| pocx-miner | `./config` | `/config` | Miner config |
| pocx-plotter | `./plots` | `/plots` | Plot files (write) |
| electrs-pocx | `./electrs-data` | `/data` | Electrs index storage |
| electrs-pocx | `./bitcoin-data` | `/root/.bitcoin` | Bitcoin node data (read) |

## Port Mappings

| Container | Host Port | Container Port | Purpose |
|-----------|-----------|----------------|---------|
| bitcoin-pocx-node | 18332 | 18332 | RPC API |
| bitcoin-pocx-node | 18333 | 18333 | P2P Network |
| electrs-pocx | 3000 | 3000 | HTTP REST API |
| electrs-pocx | 50001 | 50001 | Electrum RPC |

## Troubleshooting

### Docker Not Found

**Symptom**: "Docker is not available"

**Solution**: 
1. Install Docker (see Prerequisites)
2. Start Docker service/application
3. Verify with `docker version`

### Permission Denied (Linux)

**Symptom**: "permission denied while trying to connect to the Docker daemon"

**Solution**:
```bash
sudo usermod -aG docker $USER
# Log out and back in
```

Or run the wallet with `sudo` (not recommended).

### Container Already Exists

**Symptom**: "Container name already in use"

**Solution**:
1. Stop the existing container: `docker stop <container-name>`
2. Remove it: `docker rm <container-name>`
3. Or use the "Remove All Containers" option in the wallet

### Image Pull Failed

**Symptom**: "Failed to pull image"

**Solution**:
1. Check internet connection
2. Verify Docker is running
3. Check if GitHub Container Registry is accessible
4. Try building images locally (see above)

### WSL2 Issues (Windows)

**Symptom**: Docker commands fail on Windows

**Solution**:
1. Ensure WSL2 is installed
2. Enable WSL2 integration in Docker Desktop settings
3. Restart Docker Desktop

### Container Won't Start

**Symptom**: Container starts then immediately stops

**Solution**:
1. Check logs: `Main Menu → Docker → View Container Logs`
2. Verify configuration files exist
3. Check port conflicts: `docker ps` to see if ports are in use
4. Ensure directories have proper permissions

## Advanced Usage

### Direct Docker Commands

You can also manage containers directly with Docker CLI:

```bash
# List all containers
docker ps -a

# View logs
docker logs bitcoin-pocx-node

# Execute command in container
docker exec bitcoin-pocx-node bitcoin-cli getblockchaininfo

# Stop container
docker stop pocx-miner

# Remove container
docker rm pocx-plotter
```

### Custom Docker Compose

For advanced users, you can create a `docker-compose.yml`:

```yaml
version: '3.8'

services:
  bitcoin-pocx:
    image: ghcr.io/ev1ls33d/pocx-wallet/bitcoin-pocx:latest
    container_name: bitcoin-pocx-node
    ports:
      - "18332:18332"
      - "18333:18333"
    volumes:
      - ./bitcoin-data:/root/.bitcoin
    command: bitcoind -printtoconsole -rpcport=18332 -rpcallowip=0.0.0.0/0

  pocx-miner:
    image: ghcr.io/ev1ls33d/pocx-wallet/pocx:latest
    container_name: pocx-miner
    volumes:
      - ./plots:/plots:ro
      - ./config:/config:ro
    command: pocx_miner -c /config/config.yaml
```

Then run: `docker-compose up -d`

## Native vs Docker Comparison

| Feature | Native Binaries | Docker Containers |
|---------|----------------|-------------------|
| Setup Time | Longer (build from source) | Faster (pull images) |
| Updates | Rebuild from source | Pull new images |
| Isolation | No | Yes |
| Resource Control | Manual | Docker limits |
| Portability | Platform-specific | Cross-platform |
| Performance | Slightly faster | Minimal overhead |

## Next Steps

- Configure your miner: See `config.yaml.example`
- Create plots: Use the Plotter container
- Start mining: Use the Miner container
- Monitor blockchain: Use the Node container

For more information, see the main [README.md](README.md).

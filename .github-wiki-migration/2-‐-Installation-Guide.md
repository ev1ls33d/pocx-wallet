

This guide covers all installation options for PoCX Wallet.

## Prerequisites

### Required Software

| Software | Version | Purpose |
|----------|---------|---------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 9.0+ | Build and run the application |
| [Docker](https://www.docker.com/products/docker-desktop) | Latest | Run services (node, miner, plotter) |
| [Git](https://git-scm.com/) | Latest | Clone the repository |

### System Requirements

| Resource | Minimum | Recommended |
|----------|---------|-------------|
| RAM | 4 GB | 8 GB+ |
| Storage | 10 GB | 100 GB+ (for blockchain) |
| CPU | 2 cores | 4+ cores |

## Installation Steps

### 1. Install .NET SDK

#### Linux (Ubuntu/Debian)
```bash
# Add Microsoft package repository
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Install .NET SDK
sudo apt-get update
sudo apt-get install -y dotnet-sdk-9.0
```

#### macOS
```bash
# Using Homebrew
brew install dotnet-sdk

# Or download from https://dotnet.microsoft.com/download
```

#### Windows
Download and install from [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)

### 2. Install Docker

#### Linux
```bash
# Quick install script
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Add your user to docker group (avoids sudo)
sudo usermod -aG docker $USER
newgrp docker
```

#### macOS / Windows
Download and install [Docker Desktop](https://www.docker.com/products/docker-desktop)

### 3. Clone the Repository

```bash
git clone https://github.com/ev1ls33d/pocx-wallet.git
cd pocx-wallet
```

### 4. Build the Application

```bash
dotnet build
```

Expected output:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### 5. Run the Application

```bash
cd PocxWallet.Cli
dotnet run
```

You should see the main menu:
```
─────────────────────────────────────────
 PoCX HD Wallet
─────────────────────────────────────────

> [Wallet]         Wallet Management
  [Node]           Bitcoin-PoCX Node      ●
  [Plot]           PoCX Plotter           ●
  [Mine]           PoCX Miner             ●
  [Aggregator]     PoCX Aggregator        ●
  [Electrs]        Electrs Server         ●
  [Exit]           Exit
```

## Docker Images

The wallet uses pre-built Docker images from GitHub Container Registry:

| Image | Description |
|-------|-------------|
| `ghcr.io/ev1ls33d/pocx-wallet/bitcoin:latest` | Bitcoin-PoCX node |
| `ghcr.io/ev1ls33d/pocx-wallet/electrs:latest` | Electrum server |
| `ghcr.io/ev1ls33d/pocx-wallet/pocx:latest` | Plotter, Miner, Aggregator |

Images are automatically pulled when you start a service for the first time.

### Manual Image Pull

To pre-download images:
```bash
docker pull ghcr.io/ev1ls33d/pocx-wallet/bitcoin:latest
docker pull ghcr.io/ev1ls33d/pocx-wallet/electrs:latest
docker pull ghcr.io/ev1ls33d/pocx-wallet/pocx:latest
```

## Verifying Installation

### Check .NET Version
```bash
dotnet --version
# Should output: 9.0.x or higher
```

### Check Docker
```bash
docker --version
# Should output: Docker version 2x.x.x or higher

docker run hello-world
# Should complete successfully
```

### Test Build
```bash
cd /path/to/pocx-wallet
dotnet build
dotnet test  # Optional: run tests
```

## Troubleshooting

### .NET SDK Not Found
```
The command 'dotnet' was not found
```
**Solution**: Ensure .NET SDK is installed and in your PATH. Try logging out and back in.

### Docker Permission Denied
```
Got permission denied while trying to connect to the Docker daemon socket
```
**Solution**: 
```bash
sudo usermod -aG docker $USER
newgrp docker
# Or log out and back in
```

### Docker Not Running
```
Cannot connect to the Docker daemon
```
**Solution**: Start Docker Desktop (Windows/macOS) or `sudo systemctl start docker` (Linux)

### Build Errors
```
error CS0234: The type or namespace name 'X' does not exist
```
**Solution**: 
```bash
dotnet restore
dotnet build
```

## Next Steps

After installation:
1. Read the [Wallet Management Guide](Wallet-Management.md) to create your first wallet
2. Check the [Services Guide](Services.md) to understand Docker services
3. Review the [Configuration Guide](Configuration.md) for customization options

---

[← Back to Home](Home.md)
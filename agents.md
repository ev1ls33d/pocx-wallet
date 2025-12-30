# PoCX Wallet - Agent Technical Documentation

> **⚠️ DOCUMENTATION REQUIREMENT**: Every pull request MUST update:
> 1. This file (`agents.md`) if architecture or code changes occur
> 2. `README.md` if user-facing features change
> 3. Wiki documentation (via `/wiki` directory with PowerShell sync script)

> **⚠️ IMPORTANT**: This document MUST be updated with every pull request to reflect the latest code changes.

## Quick Start for Coding Agents

### Project Overview
- **.NET 9.0** CLI and UI applications
- **C#** with nullable reference types enabled
- Uses **Spectre.Console** for CLI UI
- Uses **Avalonia** for Desktop UI
- Uses **YamlDotNet** for YAML configuration parsing
- Uses **NBitcoin** for Bitcoin cryptography
- Manages cryptocurrency services via **Docker** or **Native** execution modes

### Project Structure

| Project | Purpose |
|---------|---------|
| `PocxWallet.Core` | Shared library: services, wallet, configuration types |
| `PocxWallet.Cli` | Command-line interface with Spectre.Console |
| `PocxWallet.UI` | Avalonia UI components (shared) |
| `PocxWallet.Desktop` | Avalonia Desktop application |

### Key Files

| File | Purpose |
|------|---------|
| `services.yaml` | Service definitions (ports, volumes, parameters, execution mode) |
| `PocxWallet.Core/Services/DockerManager.cs` | Core Docker container lifecycle management |
| `PocxWallet.Core/Services/NativeServiceManager.cs` | Core native process lifecycle management |
| `PocxWallet.Core/Services/VersionCrawlerService.cs` | Dynamic version discovery from GitHub |
| `PocxWallet.Core/Services/ServiceConfiguration.cs` | YAML model classes and data structures |
| `PocxWallet.Core/Services/BackgroundServiceManager.cs` | Background service tracking |
| `PocxWallet.Cli/Configuration/DynamicServiceMenuBuilder.cs` | CLI service menu UI and action routing |
| `PocxWallet.Cli/Configuration/ServiceDefinitionLoader.cs` | CLI YAML loading wrapper |
| `PocxWallet.Core/Wallet/HDWallet.cs` | HD wallet implementation (BIP39/BIP84) |
| `PocxWallet.Core/Address/Bech32Encoder.cs` | Bech32 address encoding (pocx1q..., tpocx1q...) |

## Architecture

### High-Level Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    PoCX Wallet Applications                  │
│               (CLI: Program.cs / Desktop: App.axaml)        │
└──────────────────────┬──────────────────────────────────────┘
                       │
       ┌───────────────┴───────────────┐
       │                               │
┌──────▼──────┐              ┌─────────▼────────┐
│   Wallet    │              │    Services      │
│  Management │              │   Management     │
│   (Core)    │              │     (Core)       │
└─────────────┘              └──────┬───────────┘
                                    │
                     ┌──────────────┴──────────────┐
                     │                             │
              ┌──────▼──────┐            ┌────────▼────────┐
              │   Docker    │            │     Native      │
              │   Manager   │            │    Service      │
              │   (Core)    │            │    Manager      │
              └─────────────┘            └─────────────────┘
```

### Execution Mode Architecture

The application supports two execution modes for services:

#### 1. Docker Mode (Default)
- Services run in Docker containers
- Uses `DockerManager` (Core) for lifecycle management
- Requires Docker installed and running
- Version management via `docker pull`
- Isolated environments with networking, volumes, and port mappings

#### 2. Native Mode
- Services run as native processes on the host
- Uses `NativeServiceManager` (Core) for lifecycle management
- Requires binaries to be downloaded and extracted
- Version management via HTTP download and extraction
- Direct host process execution with optional console window

### Service Configuration Flow

```
services.yaml (in output directory)
    │
    ├─> ServiceConfigurationLoader.Load() [Core]
    │       │
    │       ├─> Parse YAML with YamlDotNet
    │       └─> Validate configuration
    │
    ├─> CLI: DynamicServiceMenuBuilder / UI: ServiceViewModel
    │       │
    │       ├─> Check execution_mode
    │       │
    │       ├─> Docker Mode:
    │       │   ├─> DockerManager.StartContainerAsync()
    │       │   ├─> DockerManager.StopContainerAsync()
    │       │   └─> DockerManager.GetContainerLogsAsync()
    │       │
    │       └─> Native Mode:
    │           ├─> NativeServiceManager.StartNativeServiceAsync()
    │           ├─> NativeServiceManager.StopNativeServiceAsync()
    │           └─> NativeServiceManager.GetNativeServiceStatusAsync()
    │
    └─> Version Management
            │
            ├─> Docker: Pull from registry (source.docker.images)
            └─> Native: Download & extract (source.native.downloads)
```

## Data Model

### Service Definition Schema (v2.0)

```yaml
- id: "service-id"
  name: "Service Name"
  description: "Service description"
  category: "infrastructure|mining|utilities"
  execution_mode: "docker"  # or "native"
  
  container:
    image: "image-name"
    binary: "executable-name"
    container_name_default: "container-name"
    working_dir: "/path"
  
  source:
    docker:
      dynamic:
        repository: "https://github.com/owner/repo/pkgs/container/package-name"
        filter: "latest|[0-9]\\.[0-9]\\.[0-9]"
      images: []
    native:
      dynamic:
        repository: "https://github.com/owner/repo/releases/"
        filter: "linux.*\\.tar\\.gz|windows.*\\.zip"
        whitelist:
          - "binary-name"
      downloads: []
  
  ports:
    - name: "port-name"
      container_port: 8080
      host_port_default: 8080
  
  volumes:
    - name: "volume-name"
      host_path_default: "./data"
      container_path: "/data"
      read_only: false
  
  parameters:
    - name: "parameter-name"
      cli_flag: "--flag"
      type: "bool|int|string|string[]"
      default: value
```

### Key Core Classes

#### ExecutionMode Enum
```csharp
public enum ExecutionMode
{
    Docker,  // Run in Docker container
    Native   // Run as native process
}
```

#### IServiceLogger Interface
```csharp
public interface IServiceLogger
{
    void LogInfo(string message);
    void LogSuccess(string message);
    void LogWarning(string message);
    void LogError(string message);
    void LogDebug(string message);
}
```

Both CLI and UI implement this interface:
- CLI: `SpectreServiceLogger` using Spectre.Console markup
- UI: Custom implementation for Avalonia

## Key Components

### DockerManager (Core)

**Purpose**: Base Docker container lifecycle management

**Key Methods**:
- `StartContainerAsync()` - Start a container with specified configuration
- `StopContainerAsync()` - Stop a running container
- `GetContainerStatusAsync()` - Check container status
- `GetContainerLogsAsync()` - Retrieve container logs
- `ExecInContainerAsync()` - Execute commands inside container

### NativeServiceManager (Core)

**Purpose**: Base native process lifecycle management

**Key Methods**:
- `StartNativeServiceAsync()` - Spawn process with optional console window
- `StopNativeServiceAsync()` - Graceful shutdown with SIGTERM/SIGINT
- `GetNativeServiceStatusAsync()` - Check if process is alive
- `DownloadAndExtractNativeAsync()` - Download and install binaries

**Implementation Details**:
- Tracks processes in `ConcurrentDictionary<string, ProcessInfo>`
- Optional progress callback for download progress UI
- Handles `.tar.gz` and `.zip` extraction
- Applies whitelist filtering after extraction
- Platform detection via `RuntimeInformation`

### VersionCrawlerService (Core)

**Purpose**: Dynamic version discovery from GitHub

**Key Methods**:
- `CrawlGitHubReleasesAsync()` - Discover native binaries from GitHub Releases
- `CrawlContainerRegistryAsync()` - Discover Docker images from GitHub Container Registry

**Implementation Details**:
- Uses GitHub API for releases and packages
- `OnAuthenticationRequired` event for UI authentication prompts
- `SaveTokenAction` callback for token persistence
- Caches results for 5 minutes

### BackgroundServiceManager (Core)

**Purpose**: Track running background services

**Static Methods**:
- `RegisterService()` - Register a running service
- `UpdateServiceStatus()` - Update service status
- `StopService()` - Stop a specific service
- `StopAllServices()` - Stop all running services
- `GetAllServices()` - Get all registered services

## Common Development Tasks

### Adding a New Service

1. Add service definition to `services.yaml`
2. The service automatically appears in both CLI and UI (no code changes needed)

### Supporting a New UI Feature

1. Add core logic to `PocxWallet.Core`
2. Create CLI wrapper in `PocxWallet.Cli` with Spectre.Console UI
3. Create UI wrapper in `PocxWallet.UI` with Avalonia bindings

### Switching Execution Mode

In services.yaml:
```yaml
execution_mode: "native"  # or "docker"
```

Both CLI and UI honor this setting and route to appropriate managers.

## Build & Deployment

### Dependencies
- .NET 9.0 SDK
- Docker (optional, for Docker mode)
- NBitcoin (wallet cryptography)
- YamlDotNet (configuration)
- Spectre.Console (CLI UI)
- Avalonia (Desktop UI)

### Single-File Publish (CLI)
```bash
dotnet publish PocxWallet.Cli -c Release -r win-x64 --self-contained
```

### Desktop Publish
```bash
dotnet publish PocxWallet.Desktop -c Release -r win-x64
```

## Security Considerations

### Command Injection Prevention
- Container names validated with regex
- Docker command arguments use `ArgumentList` API
- No shell execution for user input

### Sensitive Data
- RPC passwords marked as `sensitive: true`
- Masked in UI
- Not logged to console or files

---

**Last Updated**: 2025-01-XX  
**Schema Version**: 2.0  
**Maintainer**: ev1ls33d

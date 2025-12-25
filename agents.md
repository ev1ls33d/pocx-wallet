# PoCX Wallet - Agent Technical Documentation

> **⚠️ DOCUMENTATION REQUIREMENT**: Every pull request MUST update:
> 1. This file (`agents.md`) if architecture or code changes occur
> 2. `README.md` if user-facing features change
> 3. Wiki documentation (via `/wiki` directory with PowerShell sync script)

> **⚠️ IMPORTANT**: This document MUST be updated with every pull request to reflect the latest code changes.

## Quick Start for Coding Agents

### Project Overview
- **.NET 9.0** CLI application
- **C#** with nullable reference types enabled
- Uses **Spectre.Console** for CLI UI
- Uses **YamlDotNet** for YAML configuration parsing
- Uses **NBitcoin** for Bitcoin cryptography
- Manages cryptocurrency services via **Docker** or **Native** execution modes

### Key Files
| File | Purpose |
|------|---------|
| `services.yaml` | Service definitions (ports, volumes, parameters, execution mode) |
| `PocxWallet.Cli/Services/DockerServiceManager.cs` | Docker container lifecycle management |
| `PocxWallet.Cli/Services/NativeServiceManager.cs` | Native process lifecycle management |
| `PocxWallet.Cli/Services/VersionCrawlerService.cs` | Dynamic version discovery from GitHub |
| `PocxWallet.Cli/Configuration/ServiceDefinition.cs` | YAML model classes and data structures |
| `PocxWallet.Cli/Configuration/DynamicServiceMenuBuilder.cs` | Service menu UI and action routing |
| `PocxWallet.Cli/Configuration/ServiceDefinitionLoader.cs` | YAML loading and validation |
| `PocxWallet.Core/Wallet/HDWallet.cs` | HD wallet implementation (BIP39/BIP84) |
| `PocxWallet.Core/Address/Bech32Address.cs` | Bech32 address encoding (pocx1q..., tpocx1q...) |

## Architecture

### High-Level Overview

```
┌─────────────────────────────────────────────────────────────┐
│                      PoCX Wallet CLI                        │
│                     (Program.cs)                            │
└──────────────────────┬──────────────────────────────────────┘
                       │
       ┌───────────────┴───────────────┐
       │                               │
┌──────▼──────┐              ┌─────────▼────────┐
│   Wallet    │              │    Services      │
│  Management │              │   Management     │
└─────────────┘              └──────┬───────────┘
                                    │
                     ┌──────────────┴──────────────┐
                     │                             │
              ┌──────▼──────┐            ┌────────▼────────┐
              │   Docker    │            │     Native      │
              │   Service   │            │    Service      │
              │   Manager   │            │    Manager      │
              └─────────────┘            └─────────────────┘
```

### Execution Mode Architecture

The application supports two execution modes for services:

#### 1. Docker Mode (Default)
- Services run in Docker containers
- Uses `DockerServiceManager` for lifecycle management
- Requires Docker installed and running
- Version management via `docker pull`
- Isolated environments with networking, volumes, and port mappings

#### 2. Native Mode
- Services run as native processes on the host
- Uses `NativeServiceManager` for lifecycle management
- Requires binaries to be downloaded and extracted
- Version management via HTTP download and extraction
- Direct host process execution with log file redirection

### Service Configuration Flow

```
services.yaml
    │
    ├─> ServiceDefinitionLoader.Load()
    │       │
    │       ├─> Parse YAML with YamlDotNet
    │       └─> Validate configuration
    │
    ├─> DynamicServiceMenuBuilder
    │       │
    │       ├─> Check execution_mode
    │       │
    │       ├─> Docker Mode:
    │       │   ├─> DockerServiceManager.StartContainerAsync()
    │       │   ├─> DockerServiceManager.StopContainerAsync()
    │       │   └─> DockerServiceManager.GetContainerLogsAsync()
    │       │
    │       └─> Native Mode:
    │           ├─> NativeServiceManager.StartNativeServiceAsync()
    │           ├─> NativeServiceManager.StopNativeServiceAsync()
    │           └─> NativeServiceManager.GetNativeServiceLogsAsync()
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
      images:
        - repository: "ghcr.io/owner/repo"
          image: "image-name"
          tag: "v1.0.0"
          description: "Version description"
    native:
      downloads:
        - url: "https://example.com/binary.tar.gz"
          version: "v1.0.0"
          platform: "linux-x64"  # linux-x64, win-x64, osx-arm64, etc.
          description: "Binary description"
          whitelist:  # Optional: files to keep after extraction
            - "binary-name"
            - "binary-cli"

### Dynamic Version Crawling

Services can be configured with dynamic source discovery instead of static URLs:

```yaml
source:
  docker:
    dynamic:
      repository: "https://github.com/owner/repo/pkgs/container/package-name"
      filter: "latest|[0-9]\\.[0-9]\\.[0-9]"  # Regex pattern for version tags
    images: []  # Legacy static images, can be empty
  native:
    dynamic:
      repository: "https://github.com/owner/repo/releases/"
      filter: "linux.*\\.tar\\.gz|windows.*\\.zip"  # Regex pattern for asset names
    downloads: []  # Legacy static downloads, can be empty
```

The `filter` is a regex pattern applied to:
- **Docker**: Version tags in container registry
- **Native**: Release asset filenames

**Dynamic source benefits**:
- Automatic discovery of new releases
- No manual YAML updates needed for each release
- Backward compatible with static sources

**Important for Agents**: When adding or modifying services, prefer `dynamic` source configuration over static `downloads`/`images` to enable automatic version discovery.
  
  ports:
    - name: "port-name"
      container_port: 8080
      host_port_default: 8080
      description: "Port description"
  
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
      description: "Parameter description"
  
  menu:
    main_menu_order: 1
    submenu:
      - action: "toggle"
        label_running: "Stop"
        label_stopped: "Start"
      - action: "logs"
        label: "View Logs"
      - action: "pull_version"
        label: "Manage Versions"
      - action: "parameters"
      - action: "settings"
```

### Key Classes

#### ServiceDefinition
- Main model class for service configuration
- Maps to YAML structure via YamlDotNet attributes
- Contains execution mode, container config, source config, ports, volumes, etc.

#### ExecutionMode Enum
```csharp
public enum ExecutionMode
{
    Docker,  // Run in Docker container
    Native   // Run as native process
}
```

#### ServiceSource
- Container class for Docker and Native source configurations
- Legacy fields maintained for backward compatibility

#### DockerSource / NativeSource
- Docker: List of available images with tags
- Native: List of downloadable binaries with platform filtering

## Key Components

### DockerServiceManager

**Purpose**: Manages Docker container lifecycle for services

**Key Methods**:
- `StartContainerAsync()` - Start a container with specified configuration
- `StopContainerAsync()` - Stop a running container
- `GetContainerStatusAsync()` - Check container status
- `GetContainerLogsAsync()` - Retrieve container logs
- `ExecInContainerAsync()` - Execute commands inside container

**Implementation Details**:
- Uses `docker` CLI via `Process` API
- Handles network creation/management
- Manages port mappings and volume mounts
- Supports GPU passthrough for OpenCL
- Validates container names to prevent injection

### NativeServiceManager

**Purpose**: Manages native process lifecycle for services

**Key Methods**:
- `StartNativeServiceAsync()` - Spawn process with redirected I/O
- `StopNativeServiceAsync()` - Graceful shutdown with SIGTERM/SIGINT
- `GetNativeServiceStatusAsync()` - Check if process is alive
- `GetNativeServiceLogsAsync()` - Read log files
- `DownloadAndExtractNativeAsync()` - Download and install binaries

**Implementation Details**:
- Tracks processes in `ConcurrentDictionary<string, ProcessInfo>`
- Redirects stdout/stderr to log files in `logs/` directory
- Handles `.tar.gz` and `.zip` extraction
- Applies whitelist filtering after extraction
- Platform detection via `RuntimeInformation`
- Graceful shutdown with 5-second timeout before force kill

### DynamicServiceMenuBuilder

**Purpose**: Builds and manages interactive CLI menus from service definitions

**Key Methods**:
- `ShowServiceMenuAsync()` - Display service submenu
- `StartServiceAsync()` - Start service (routes to Docker/Native manager)
- `StopServiceAsync()` - Stop service (routes to Docker/Native manager)
- `ViewServiceLogsAsync()` - View logs (routes to Docker/Native manager)
- `ShowVersionManagementAsync()` - Manage service versions

**Implementation Details**:
- Routes actions based on `execution_mode`
- Builds dynamic menus from `services.yaml` configuration
- Handles parameter editing and persistence
- Manages environment variables and port/volume overrides

### VersionCrawlerService

**Purpose**: Dynamically discovers service versions from GitHub repositories and container registries

**Key Methods**:
- `CrawlGitHubReleasesAsync()` - Discover native binaries from GitHub Releases
- `CrawlContainerRegistryAsync()` - Discover Docker images from GitHub Container Registry

**Implementation Details**:
- Uses GitHub API for releases and packages
- Applies regex filtering to version tags and asset names
- Auto-detects platform from filename patterns (linux, windows, x64, arm64)
- Caches results for 5 minutes to avoid repeated API calls
- Provides fallback to common tags when API fails
- Parses GitHub URLs to extract owner/repo/package information

## Common Development Tasks

### Adding a New Service

1. Add service definition to `services.yaml`:
```yaml
- id: "my-service"
  name: "My Service"
  execution_mode: "docker"  # or "native"
  container:
    image: "my-service"
    binary: "my-service-bin"
  source:
    docker:
      images:
        - repository: "ghcr.io/org/repo"
          image: "my-service"
          tag: "latest"
    native:
      downloads:
        - url: "https://example.com/my-service-linux.tar.gz"
          version: "v1.0"
          platform: "linux-x64"
  # ... ports, volumes, parameters, etc.
```

2. The service will automatically appear in the CLI menu (no code changes needed)

### Adding a New Parameter Type

1. Update `ServiceParameter.Type` handling in `DynamicServiceMenuBuilder.BuildCommand()`
2. Add UI handling in `DynamicServiceMenuBuilder.SetParameterValue()`

### Supporting a New Archive Format

1. Add extraction logic to `NativeServiceManager.DownloadAndExtractNativeAsync()`
2. Update file extension check

### Adding Custom Actions

1. Define custom action in `services.yaml`:
```yaml
submenu:
  - action: "custom"
    id: "my-action"
    label: "Do Something"
    command:
      binary: "bitcoin-cli"
      arguments: ["getblockchaininfo"]
      show_output: true
```

2. Handler already exists in `DynamicServiceMenuBuilder.HandleCustomActionAsync()`

## Testing

### Build and Run
```bash
# Build
dotnet build PocxWallet.sln

# Run
cd PocxWallet.Cli
dotnet run
```

### Manual Testing Checklist

**Docker Mode**:
- [ ] Start service via menu
- [ ] View logs
- [ ] Stop service
- [ ] Pull new Docker image
- [ ] Edit parameters
- [ ] Verify container created with correct config

**Native Mode**:
- [ ] Set `execution_mode: "native"` in services.yaml
- [ ] Download binary via version management
- [ ] Start service
- [ ] Verify process running and logs created
- [ ] Stop service gracefully
- [ ] Verify whitelist filtering works

**Version Management**:
- [ ] Docker: Pull different image versions
- [ ] Native: Download binaries for current platform
- [ ] Native: Verify platform filtering works
- [ ] Native: Verify extraction and whitelist filtering

### Common Issues

**Docker not available**:
- Ensure Docker is installed and running
- Check Docker socket permissions on Linux

**Binary not found (Native mode)**:
- Download binary via "Manage Versions"
- Check file permissions (chmod +x on Linux)
- Verify platform matches download

**Permission denied (Native mode)**:
- On Linux: `chmod +x <binary>`
- On Windows: Run as administrator if needed

## Code Conventions

### Naming
- Services: kebab-case IDs (`bitcoin-node`, `miner`)
- Classes: PascalCase (`ServiceDefinition`)
- Methods: PascalCase (`StartServiceAsync`)
- Private fields: _camelCase (`_dockerManager`)

### Async/Await
- All I/O operations are async
- Method names end with `Async`
- Use `ConfigureAwait(false)` for library code (not currently applied)

### Error Handling
- Display user-friendly messages with Spectre.Console
- Use `try-catch` for external process calls
- Log errors to console, not files (CLI app)

### YAML Serialization
- Use `YamlDotNet` attributes
- `[YamlMember(Alias = "snake_case")]` for properties
- Maintain backward compatibility with legacy fields

## Build & Deployment

### Dependencies
- .NET 9.0 SDK
- Docker (optional, for Docker mode)
- NBitcoin (wallet cryptography)
- Spectre.Console (CLI UI)
- YamlDotNet (configuration)

### Single-File Publish
```bash
# Linux
dotnet publish -c Release -r linux-x64 --self-contained

# Windows
dotnet publish -c Release -r win-x64 --self-contained

# macOS
dotnet publish -c Release -r osx-x64 --self-contained
```

### Docker Images
GitHub Actions builds and publishes Docker images:
- `ghcr.io/ev1ls33d/pocx-wallet/bitcoin:latest`
- `ghcr.io/ev1ls33d/pocx-wallet/electrs:latest`
- `ghcr.io/ev1ls33d/pocx-wallet/pocx:latest`

## Security Considerations

### Command Injection Prevention
- Container names validated with regex
- Docker command arguments use `ArgumentList` API
- No shell execution for user input

### Sensitive Data
- RPC passwords marked as `sensitive: true`
- Masked in UI with asterisks
- Not logged to console or files

### Process Isolation
- Docker containers run with standard Docker security
- Native processes inherit user permissions
- No privilege escalation

## Contributing

### Pull Request Guidelines
1. Update `agents.md` with any architectural changes
2. Add/update documentation in `wiki/` directory
3. Test both Docker and Native modes if applicable
4. Ensure backward compatibility with existing `services.yaml`
5. Run `dotnet build` to verify no compilation errors

### Code Review Checklist
- [ ] No hardcoded credentials or secrets
- [ ] User input validated and sanitized
- [ ] Error messages are user-friendly
- [ ] Async methods properly awaited
- [ ] Resource cleanup (Dispose, using statements)
- [ ] YAML schema changes documented

## Resources

### External Documentation
- [Spectre.Console](https://spectreconsole.net/) - CLI framework
- [YamlDotNet](https://github.com/aaubry/YamlDotNet) - YAML parser
- [NBitcoin](https://github.com/MetacoSA/NBitcoin) - Bitcoin library
- [PoC Consortium](https://github.com/PoC-Consortium) - PoCX project

### Internal Wiki
See `wiki/` directory for detailed user documentation:
- Installation guide
- Service configuration
- Wallet management
- Mining & plotting guides
- Troubleshooting

### Updating Documentation

The wiki is stored in a separate repository but mirrored in `/wiki`:

1. Edit files in `/wiki/*.md`
2. Run `.\wiki\sync-wiki.ps1` to push to the wiki (requires PowerShell and git access)
3. Alternatively, changes in `/wiki` will be synced by maintainers

**PowerShell Sync Script**:
```powershell
# From repository root
.\wiki\sync-wiki.ps1

# With custom commit message
.\wiki\sync-wiki.ps1 -CommitMessage "Update installation guide"
```

---

**Last Updated**: 2025-12-25  
**Schema Version**: 2.0  
**Maintainer**: ev1ls33d

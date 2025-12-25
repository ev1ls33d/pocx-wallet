# Execution Modes

PoCX Wallet supports two execution modes for services: **Docker** and **Native**.

## Docker Mode (Default)

### Overview
Services run in isolated Docker containers with their own filesystem, networking, and resources.

### Advantages
- ✅ **Consistent Environment**: Same behavior across all platforms
- ✅ **Easy Updates**: Pull new images with one command
- ✅ **Isolation**: Services don't interfere with host system
- ✅ **Networking**: Built-in Docker networking between services
- ✅ **Security**: Container isolation provides additional security

### Disadvantages
- ❌ **Docker Required**: Must have Docker installed and running
- ❌ **Resource Overhead**: Containers use more memory/disk
- ❌ **Complexity**: Additional layer between service and host

### Use Cases
- Development and testing
- Production deployments
- Multi-service setups with networking
- Platforms where Docker is readily available

## Native Mode

### Overview
Services run as native processes directly on the host operating system.

### Advantages
- ✅ **No Docker Required**: Works without Docker installation
- ✅ **Lower Overhead**: Direct process execution uses fewer resources
- ✅ **Direct Access**: Full access to host hardware and filesystem
- ✅ **Performance**: Potentially better performance for I/O intensive tasks

### Disadvantages
- ❌ **Manual Management**: Must download and manage binaries
- ❌ **Platform Specific**: Binaries must match OS and architecture
- ❌ **Less Isolation**: Services run with user's permissions
- ❌ **Updates**: Manual version management required

### Use Cases
- Resource-constrained systems (Raspberry Pi, etc.)
- Systems without Docker (or where Docker is difficult to install)
- Direct hardware access requirements
- Minimal overhead deployments

## Switching Between Modes

### Step 1: Edit services.yaml

Open `services.yaml` and change the `execution_mode` for the desired service:

```yaml
- id: "bitcoin-node"
  execution_mode: "native"  # Change from "docker" to "native"
```

### Step 2: Download Binary (Native Mode Only)

If switching to native mode:

1. Open PoCX Wallet CLI
2. Navigate to the service menu (e.g., `[Node]`)
3. Select **"Manage Versions"**
4. Choose a version compatible with your platform
5. Wait for download and extraction to complete

### Step 3: Start Service

The service will now run in the selected mode:
- **Docker**: Container is created and started
- **Native**: Process is spawned on the host

## Version Management

### Docker Mode

**Pulling New Images**:
1. Navigate to service menu
2. Select "Manage Versions"
3. Choose Docker image from list
4. Image is pulled from registry
5. Service configuration updated automatically

**Available images** are defined in `services.yaml`:
```yaml
source:
  docker:
    images:
      - repository: "ghcr.io/ev1ls33d/pocx-wallet"
        image: "bitcoin"
        tag: "v30-RC3"
        description: "Bitcoin-PoCX v30 Release Candidate 3"
      - repository: "ghcr.io/ev1ls33d/pocx-wallet"
        image: "bitcoin"
        tag: "latest"
        description: "Latest development build"
```

### Native Mode

**Downloading Binaries**:
1. Navigate to service menu
2. Select "Manage Versions"
3. Choose binary version (automatically filtered by platform)
4. Binary downloads and extracts to `./service-id/` directory
5. Files outside whitelist are removed (if specified)

**Available downloads** are defined in `services.yaml`:
```yaml
source:
  native:
    downloads:
      - url: "https://github.com/.../binary-linux-x64.tar.gz"
        version: "v30-RC3"
        platform: "linux-x64"
        description: "Binary for Linux x64"
        whitelist:
          - "binary"
          - "binary-cli"
```

### Platform Detection

The wallet automatically detects your platform:
- **linux-x64**: Linux on x86_64
- **linux-arm64**: Linux on ARM64
- **win-x64**: Windows on x86_64
- **win-arm64**: Windows on ARM64
- **osx-x64**: macOS on x86_64
- **osx-arm64**: macOS on Apple Silicon

Only downloads matching your platform are shown in the menu.

### Whitelist Filtering

When downloading native binaries, you can specify a whitelist of files to keep:

```yaml
whitelist:
  - "bitcoind"
  - "bitcoin-cli"
```

After extraction, **all files not in the whitelist are deleted**. This is useful when:
- Archive contains multiple binaries but you only need specific ones
- Archive includes documentation, tests, or other unnecessary files
- Different services use binaries from the same archive

## Logs

### Docker Mode

Logs are retrieved from Docker:
```bash
docker logs <container-name>
```

Accessible via CLI: **Service Menu → View Logs**

### Native Mode

Logs are written to files in the `logs/` directory:
- `logs/<service-id>.log` - Standard output
- `logs/<service-id>.error.log` - Standard error

Accessible via CLI: **Service Menu → View Logs**

## Process Management

### Docker Mode

- **Start**: `docker run` with configured settings
- **Stop**: `docker stop` with graceful timeout
- **Status**: Check container state with `docker inspect`

### Native Mode

- **Start**: Spawn process with `System.Diagnostics.Process`
- **Stop**: Send SIGTERM (Unix) / CloseMainWindow (Windows)
- **Force Kill**: After 5-second timeout if graceful shutdown fails
- **Status**: Check if process is still alive

## Troubleshooting

### Docker Mode Issues

**Container won't start**:
- Check Docker is running: `docker ps`
- Check Docker daemon logs
- Verify image exists: `docker images`

**Permission errors**:
- Linux: Add user to `docker` group
- Windows: Run Docker Desktop as Administrator

### Native Mode Issues

**Binary not found**:
- Download binary via "Manage Versions"
- Verify extraction completed successfully
- Check `./service-id/` directory exists

**Permission denied**:
- Linux/macOS: `chmod +x ./service-id/binary`
- Windows: Run as Administrator

**Process exits immediately**:
- Check error log: `logs/service-id.error.log`
- Verify binary is compatible with platform
- Check required dependencies are installed

## Best Practices

### Docker Mode
- Use specific version tags, not `latest` for production
- Monitor container resource usage
- Regularly pull updates
- Use Docker volumes for persistent data

### Native Mode
- Keep binaries updated
- Monitor process resource usage
- Backup binary directories before updates
- Set up proper file permissions
- Use dedicated user accounts for services (production)

## Comparison Table

| Feature | Docker Mode | Native Mode |
|---------|-------------|-------------|
| Setup Complexity | Medium | Low |
| Resource Usage | Higher | Lower |
| Isolation | Strong | Weak |
| Platform Support | All | Platform-specific |
| Update Process | Pull image | Download binary |
| Networking | Docker network | Host network |
| Storage | Volumes | Direct filesystem |
| Security | Container isolation | User permissions |

## Migration

### Docker → Native

1. Stop Docker service
2. Export data from Docker volume (if needed)
3. Change `execution_mode` to `"native"`
4. Download binary via "Manage Versions"
5. Copy data to native service directory
6. Start native service

### Native → Docker

1. Stop native service
2. Backup native service directory
3. Change `execution_mode` to `"docker"`
4. Pull Docker image via "Manage Versions"
5. Copy data to Docker volume (if needed)
6. Start Docker service

---

**Next**: [Version Management](Version-Management.md)  
**Previous**: [Services Overview](Services.md)

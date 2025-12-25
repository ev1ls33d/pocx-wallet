# Settings Menu Structure (Post-Refactor)

This document shows the refactored settings menu structure that adapts based on execution mode.

## Menu Structure

```
Main Menu → [Service] → Settings
```

### Settings Menu Layout

#### First Entry (Always Shown)
```
Execution Mode         [Docker|Native]
```

#### Docker Mode Settings (in order)
```
Execution Mode         Docker
Container Name         pocx-node
Environment            3 configured
Volumes                1 configured  
Ports                  4 configured
Network                pocx
<= Back
```

**Actions:**
- **Execution Mode**: Switch between Docker and Native
- **Container Name**: Edit the Docker container name
- **Environment**: Manage environment variables (add/edit/remove)
- **Volumes**: Configure volume mounts (host path mapping)
- **Ports**: Configure port mappings (host:container)
- **Network**: Edit Docker network name

#### Native Mode Settings
```
Execution Mode         Native
Spawn New Console      true
<= Back
```

**Actions:**
- **Execution Mode**: Switch between Docker and Native
- **Spawn New Console**: Toggle between spawning a new console window (true) or redirecting output to log file (false)
  - `true`: Service spawns in a new console window
  - `false`: Service output is redirected to log files (viewable via "View Logs")

## Implementation Details

### ServiceDefinition.cs
- Added `spawn_new_console` property (default: `true`)
- Stored in services.yaml and persists across restarts

### DynamicServiceMenuBuilder.cs
- Refactored `ShowServiceSettings()` method
- Menu adapts based on `service.GetExecutionMode()`
- Added `EditExecutionMode()` method
- Added `EditSpawnNewConsole()` method
- Removed deprecated repository/tag settings (now handled via "Manage Versions")

### Strings.cs
- Added `ExecutionMode` constant
- Added `SpawnNewConsole` constant

## User Experience

### Switching Execution Mode
1. User selects "Execution Mode" in settings
2. Presented with choice: Docker or Native
3. Selection is saved to services.yaml
4. User is reminded to download binaries (Native) or pull images (Docker) via "Manage Versions"

### Configuring Native Mode Console
1. In Native mode, user can toggle "Spawn New Console"
2. **True** (default): Service runs in separate console window
   - Good for interactive services
   - Console output visible in real-time
3. **False**: Service output redirected to log files
   - Good for background services
   - Logs viewable via "View Logs" menu

## Code Changes Summary

### Files Modified
1. `PocxWallet.Cli/Configuration/ServiceDefinition.cs`
   - Added `SpawnNewConsole` property with YAML binding

2. `PocxWallet.Cli/Resources/Strings.cs`
   - Added settings menu strings for new options

3. `PocxWallet.Cli/Configuration/DynamicServiceMenuBuilder.cs`
   - Completely refactored `ShowServiceSettings()` method
   - Different menu items based on execution mode
   - Removed deprecated settings (repository, tag)
   - Added execution mode switching
   - Added spawn new console toggle

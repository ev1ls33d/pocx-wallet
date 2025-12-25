using PocxWallet.Cli.Resources;
using PocxWallet.Cli.Services;
using PocxWallet.Core.Wallet;
using Spectre.Console;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PocxWallet.Cli.Configuration;

/// <summary>
/// Builds and manages dynamic menus from service definitions loaded from services.yaml
/// </summary>
public class DynamicServiceMenuBuilder
{
    private readonly DockerServiceManager _dockerManager;
    private readonly NativeServiceManager _nativeManager;
    private readonly ServiceConfiguration? _serviceConfig;
    private readonly CommandTemplateEngine _templateEngine;
    private readonly Func<HDWallet?> _walletProvider;
    private readonly VersionCrawlerService _versionCrawler;

    public DynamicServiceMenuBuilder(
        ServiceConfiguration? serviceConfig, 
        DockerServiceManager dockerManager,
        Func<HDWallet?>? walletProvider = null)
    {
        _serviceConfig = serviceConfig;
        _dockerManager = dockerManager;
        _nativeManager = new NativeServiceManager();
        _walletProvider = walletProvider ?? (() => null);
        _templateEngine = new CommandTemplateEngine(_walletProvider);
        _versionCrawler = new VersionCrawlerService();
    }

    /// <summary>
    /// Get enabled services from the configuration
    /// </summary>
    public List<ServiceDefinition> GetEnabledServices()
    {
        return ServiceDefinitionLoader.GetEnabledServices(_serviceConfig);
    }

    /// <summary>
    /// Get service status as colored LED indicator
    /// </summary>
    public async Task<string> GetServiceStatusIndicatorAsync(ServiceDefinition service)
    {
        var status = await GetServiceStatusAsync(service);
        return status == "running" ? "[green]●[/]" : "[red]●[/]";
    }

    /// <summary>
    /// Check if service is running
    /// </summary>
    public async Task<bool> IsServiceRunningAsync(ServiceDefinition service)
    {
        var status = await GetServiceStatusAsync(service);
        return status == "running";
    }
    
    /// <summary>
    /// Get service status (works for both Docker and Native modes)
    /// </summary>
    private async Task<string> GetServiceStatusAsync(ServiceDefinition service)
    {
        var mode = service.GetExecutionMode();
        
        if (mode == ExecutionMode.Native)
        {
            return await _nativeManager.GetNativeServiceStatusAsync(service.Id);
        }
        else
        {
            var containerName = GetContainerName(service);
            return await _dockerManager.GetContainerStatusAsync(containerName);
        }
    }

    /// <summary>
    /// Get the container name for a service (uses override if set)
    /// </summary>
    public string GetContainerName(ServiceDefinition service)
    {
        // First check for user override
        if (!string.IsNullOrEmpty(service.ContainerNameOverride))
        {
            return service.ContainerNameOverride;
        }

        // Check for default container name in services.yaml
        if (!string.IsNullOrEmpty(service.Container?.ContainerNameDefault))
        {
            return service.Container.ContainerNameDefault;
        }
        
        // Fallback to default naming convention
        return $"pocx-{service.Id}";
    }

    /// <summary>
    /// Get image name for a service
    /// </summary>
    public string GetImageName(ServiceDefinition service)
    {
        return service.Container?.Image ?? service.Id;
    }

    /// <summary>
    /// Build port mappings dictionary for Docker using overrides when available
    /// </summary>
    public Dictionary<int, int> BuildPortMappings(ServiceDefinition service)
    {
        var mappings = new Dictionary<int, int>();
        
        if (service.Ports == null)
        {
            return mappings;
        }

        foreach (var port in service.Ports)
        {
            if (port.Optional)
            {
                continue; // Skip optional ports for now
            }

            var hostPort = GetPortValue(service, port);
            mappings[hostPort] = port.ContainerPort;
        }

        return mappings;
    }

    /// <summary>
    /// Build volume mappings dictionary for Docker using overrides when available
    /// </summary>
    public Dictionary<string, string> BuildVolumeMappings(ServiceDefinition service)
    {
        var mappings = new Dictionary<string, string>();
        
        if (service.Volumes == null)
        {
            return mappings;
        }

        foreach (var volume in service.Volumes)
        {
            var hostPath = GetVolumePath(service, volume);
            if (!string.IsNullOrEmpty(hostPath))
            {
                mappings[hostPath] = volume.ContainerPath;
            }
        }

        return mappings;
    }

    /// <summary>
    /// Get list of read-only volume paths
    /// </summary>
    public List<string> GetReadOnlyVolumes(ServiceDefinition service)
    {
        var readOnlyPaths = new List<string>();
        
        if (service.Volumes == null)
        {
            return readOnlyPaths;
        }

        foreach (var volume in service.Volumes.Where(v => v.ReadOnly))
        {
            var hostPath = GetVolumePath(service, volume);
            if (!string.IsNullOrEmpty(hostPath))
            {
                readOnlyPaths.Add(hostPath);
            }
        }

        return readOnlyPaths;
    }

    /// <summary>
    /// Build environment variables dictionary for Docker from service definition
    /// </summary>
    public Dictionary<string, string> BuildEnvironmentVariables(ServiceDefinition service)
    {
        var envVars = new Dictionary<string, string>();
        
        if (service.Environment == null)
        {
            return envVars;
        }

        foreach (var envVar in service.Environment)
        {
            if (!string.IsNullOrEmpty(envVar.Name))
            {
                // Use override value if set, otherwise use default value
                var value = envVar.ValueOverride ?? envVar.Value;
                if (!string.IsNullOrEmpty(value))
                {
                    envVars[envVar.Name] = value;
                }
            }
        }

        return envVars;
    }

    /// <summary>
    /// Build command string from parameters
    /// </summary>
    public string? BuildCommand(ServiceDefinition service)
    {
        var commands = new List<string>();
        
        // Add the binary executable first (e.g., "bitcoind", "electrs")
        if (!string.IsNullOrEmpty(service.Container?.Binary))
        {
            commands.Add(service.Container.Binary);
        }
        
        // Add the base command if specified (legacy support)
        if (!string.IsNullOrEmpty(service.Container?.Command))
        {
            commands.Add(service.Container.Command);
        }

        // Add user-set parameters in order of appearance
        // Parameters with a 'value' node are passed to the container as CLI flags
        if (service.Parameters != null)
        {
            foreach (var param in service.Parameters.Where(p => !p.Hidden && p.HasUserValue))
            {
                var cliFlag = param.CliFlag;
                if (string.IsNullOrEmpty(cliFlag)) continue;

                // Determine if this parameter uses equals sign syntax
                // Default: bool types don't use equals, other types do
                var useEquals = param.UseEquals ?? false;

                switch (param.Type.ToLower())
                {
                    case "bool":
                        // Boolean: if value is true, add the flag; if false, skip it
                        var boolValue = param.Value?.ToString()?.ToLower() == "true";
                        if (boolValue)
                        {
                            commands.Add(cliFlag);
                        }
                        break;
                    
                    case "int":
                        // Integer: add flag=value or flag value
                        var intValue = param.Value?.ToString();
                        if (!string.IsNullOrEmpty(intValue))
                        {
                            commands.Add(useEquals ? $"{cliFlag}={intValue}" : $"{cliFlag} {intValue}");
                        }
                        break;
                    
                    case "string[]":
                        // String array: add flag=value or flag value for each item
                        if (param.Value is IEnumerable<object> enumerable)
                        {
                            foreach (var item in enumerable)
                            {
                                var itemStr = item?.ToString();
                                if (!string.IsNullOrEmpty(itemStr))
                                {
                                    commands.Add(useEquals ? $"{cliFlag}={itemStr}" : $"{cliFlag} {itemStr}");
                                }
                            }
                        }
                        else if (param.Value is string strArray)
                        {
                            // Handle comma-separated string format
                            foreach (var item in strArray.Split(',', StringSplitOptions.RemoveEmptyEntries))
                            {
                                commands.Add(useEquals ? $"{cliFlag}={item.Trim()}" : $"{cliFlag} {item.Trim()}");
                            }
                        }
                        break;
                    
                    case "string":
                    default:
                        // String: add flag=value or flag value
                        var strValue = param.Value?.ToString();
                        if (!string.IsNullOrEmpty(strValue))
                        {
                            commands.Add(useEquals ? $"{cliFlag}={strValue}" : $"{cliFlag} {strValue}");
                        }
                        break;
                }
            }
        }

        return commands.Count > 0 ? string.Join(" ", commands) : null;
    }

    /// <summary>
    /// Show service submenu and handle actions
    /// </summary>
    public async Task ShowServiceMenuAsync(ServiceDefinition service, Action showBanner)
    {
        bool back = false;
        while (!back)
        {
            var isRunning = await IsServiceRunningAsync(service);
            var statusIndicator = isRunning ? Strings.Status.Running : Strings.Status.Stopped;
            
            // Build dynamic submenu from service definition
            var choices = new List<string>();
            var mode = service.GetExecutionMode();
            
            if (service.Menu?.Submenu != null)
            {
                foreach (var item in service.Menu.Submenu)
                {
                    // Skip logs in native mode
                    if (item.Action == "logs" && mode == ExecutionMode.Native)
                    {
                        continue;
                    }
                    
                    var label = item.Action switch
                    {
                        "toggle" => isRunning ? (item.LabelRunning ?? Strings.ServiceMenu.StopService) : (item.LabelStopped ?? Strings.ServiceMenu.StartService),
                        "logs" => item.Label ?? Strings.ServiceMenu.ViewLogs,
                        "parameters" => item.Label ?? Strings.ServiceMenu.Parameters,
                        "settings" => item.Label ?? Strings.ServiceMenu.Settings,
                        "custom" => item.Label ?? item.Id ?? "Custom Action",
                        _ => item.Label ?? item.Action
                    };
                    choices.Add(label);
                }
            }
            else
            {
                // Default menu if not defined
                choices.Add(isRunning ? Strings.ServiceMenu.StopService : Strings.ServiceMenu.StartService);
                
                // Skip View Logs in native mode
                if (mode != ExecutionMode.Native)
                {
                    choices.Add(Strings.ServiceMenu.ViewLogs);
                }
                
                choices.Add(Strings.ServiceMenu.Parameters);
                choices.Add(Strings.ServiceMenu.Settings);
            }
            
            choices.Add(Strings.ServiceMenu.Back);

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title(string.Format(Strings.ServiceMenu.TitleFormat, service.Name, GetContainerName(service), statusIndicator))
                    .PageSize(10)
                    .AddChoices(choices)
            );

            AnsiConsole.Clear();
            showBanner();

            if (choice == Strings.ServiceMenu.Back)
            {
                back = true;
                continue;
            }

            // Find the corresponding submenu item
            var selectedItem = FindSubmenuItem(service, choice, isRunning);
            if (selectedItem != null)
            {
                await HandleSubmenuActionAsync(service, selectedItem, isRunning, showBanner);
            }

            if (!back && selectedItem?.Action != "settings" && selectedItem?.Action != "parameters")
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine(Strings.ServiceMenu.PressEnterToContinue);
                Console.ReadLine();
                AnsiConsole.Clear();
                showBanner();
            }
        }
    }

    private SubmenuItem? FindSubmenuItem(ServiceDefinition service, string choice, bool isRunning)
    {
        if (service.Menu?.Submenu == null)
        {
            // Return synthetic item for default menu
            if (choice.Contains("Stop") || choice.Contains("Start"))
            {
                return new SubmenuItem { Action = "toggle" };
            }
            if (choice == Strings.ServiceMenu.ViewLogs)
            {
                return new SubmenuItem { Action = "logs" };
            }
            if (choice == Strings.ServiceMenu.Parameters)
            {
                return new SubmenuItem { Action = "parameters" };
            }
            if (choice == Strings.ServiceMenu.Settings)
            {
                return new SubmenuItem { Action = "settings" };
            }
            return null;
        }

        foreach (var item in service.Menu.Submenu)
        {
            var label = item.Action switch
            {
                "toggle" => isRunning ? (item.LabelRunning ?? Strings.ServiceMenu.StopService) : (item.LabelStopped ?? Strings.ServiceMenu.StartService),
                "logs" => item.Label ?? Strings.ServiceMenu.ViewLogs,
                "parameters" => item.Label ?? Strings.ServiceMenu.Parameters,
                "settings" => item.Label ?? Strings.ServiceMenu.Settings,
                "custom" => item.Label ?? item.Id ?? "Custom Action",
                _ => item.Label ?? item.Action
            };

            if (label == choice)
            {
                return item;
            }
        }

        return null;
    }

    private async Task HandleSubmenuActionAsync(ServiceDefinition service, SubmenuItem item, bool isRunning, Action showBanner)
    {
        switch (item.Action)
        {
            case "toggle":
                if (isRunning)
                {
                    await StopServiceAsync(service);
                }
                else
                {
                    await StartServiceAsync(service);
                }
                break;

            case "logs":
                await ViewServiceLogsAsync(service);
                break;

            case "pull_version":
                await ShowVersionManagementAsync(service, showBanner);
                break;

            case "parameters":
                ShowServiceParameters(service, showBanner);
                break;

            case "settings":
                await ShowServiceSettings(service, showBanner);
                break;

            case "custom":
                await HandleCustomActionAsync(service, item);
                break;
        }
    }

    /// <summary>
    /// Start a service (Docker or Native based on execution_mode)
    /// </summary>
    public async Task StartServiceAsync(ServiceDefinition service)
    {
        var mode = service.GetExecutionMode();
        
        if (mode == ExecutionMode.Native)
        {
            await StartNativeServiceAsync(service);
        }
        else
        {
            await StartDockerServiceAsync(service);
        }
    }

    /// <summary>
    /// Start a Docker service container using settings from services.yaml
    /// </summary>
    private async Task StartDockerServiceAsync(ServiceDefinition service)
    {
        var containerName = GetContainerName(service);
        var imageName = GetImageName(service);
        var repository = GetServiceRepository(service);
        var tag = GetServiceTag(service);
        var network = GetServiceNetwork(service);

        AnsiConsole.MarkupLine(string.Format(Strings.Container.StartingFormat, service.Name));

        // Ensure network exists
        await _dockerManager.EnsureNetworkExistsAsync(network);

        // Build volume mappings from service settings
        var volumes = BuildVolumeMappings(service);
        
        // Ensure volume directories/files exist
        if (service.Volumes != null)
        {
            foreach (var volume in service.Volumes)
            {
                var hostPath = GetVolumePath(service, volume);
                if (string.IsNullOrEmpty(hostPath)) continue;
                
                if (!Directory.Exists(hostPath) && !File.Exists(hostPath))
                {
                    try
                    {
                        if (volume.IsFile)
                        {
                            // For file volumes, create the parent directory
                            var parentDir = Path.GetDirectoryName(hostPath);
                            if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
                            {
                                Directory.CreateDirectory(parentDir);
                                AnsiConsole.MarkupLine(string.Format(Strings.Container.CreatedDirectoryFormat, Markup.Escape(parentDir)));
                            }
                        }
                        else
                        {
                            Directory.CreateDirectory(hostPath);
                            AnsiConsole.MarkupLine(string.Format(Strings.Container.CreatedDirectoryFormat, Markup.Escape(hostPath)));
                        }
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine(string.Format(Strings.Container.CouldNotCreatePathFormat, Markup.Escape(hostPath), Markup.Escape(ex.Message)));
                    }
                }
            }
        }

        var ports = BuildPortMappings(service);
        var readOnlyVolumes = GetReadOnlyVolumes(service);
        var command = BuildCommand(service);
        var gpuPassthrough = IsGpuPassthroughEnabled(service);
        var environmentVars = BuildEnvironmentVariables(service);

        var success = await _dockerManager.StartContainerAsync(
            containerName,
            imageName,
            repository,
            tag,
            environmentVars: environmentVars.Count > 0 ? environmentVars : null,
            volumeMounts: volumes.Count > 0 ? volumes : null,
            portMappings: ports.Count > 0 ? ports : null,
            command: command,
            network: network,
            readOnlyVolumes: readOnlyVolumes.Count > 0 ? readOnlyVolumes : null,
            gpuPassthrough: gpuPassthrough
        );

        if (success)
        {
            AnsiConsole.MarkupLine(string.Format(Strings.Container.StartedSuccessFormat, service.Name));
        }
        else
        {
            AnsiConsole.MarkupLine(string.Format(Strings.Container.StartFailedFormat, service.Name));
        }

        // Show last 5 log lines after starting (both success and failure)
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Last 5 log lines:[/]");
        await _dockerManager.DisplayContainerLogsAsync(containerName, 5);
    }

    /// <summary>
    /// Start a native service process
    /// </summary>
    private async Task StartNativeServiceAsync(ServiceDefinition service)
    {
        // Determine binary path
        var serviceDir = Path.Combine(".", service.Id);
        var binaryName = service.Container?.Binary ?? service.Id;
        
        // Add .exe extension on Windows
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !binaryName.EndsWith(".exe"))
        {
            binaryName += ".exe";
        }
        
        var binaryPath = Path.Combine(serviceDir, binaryName);

        // Build command arguments
        var command = BuildCommand(service);
        var arguments = command?.Replace(service.Container?.Binary ?? "", "").Trim();
        
        // Build environment variables
        var environmentVars = BuildEnvironmentVariables(service);
                
        // Get spawn new console setting
        var spawnNewConsole = service.SpawnNewConsole;

        var success = await _nativeManager.StartNativeServiceAsync(
            service.Id,
            service.Name,
            binaryName,
            arguments,
            serviceDir,
            environmentVars.Count > 0 ? environmentVars : null,
            spawnNewConsole
        );

        if (!success)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[yellow]Tip: Use 'Manage Versions' to download the binary if it's not already installed[/]");
        }
    }

    /// <summary>
    /// Stop a service (Docker or Native)
    /// </summary>
    public async Task StopServiceAsync(ServiceDefinition service)
    {
        var mode = service.GetExecutionMode();
        
        if (mode == ExecutionMode.Native)
        {
            await _nativeManager.StopNativeServiceAsync(service.Id, service.Name);
        }
        else
        {
            var containerName = GetContainerName(service);
            AnsiConsole.MarkupLine(string.Format(Strings.Container.StoppingFormat, service.Name));

            var success = await _dockerManager.StopContainerAsync(containerName);

            if (success)
            {
                AnsiConsole.MarkupLine(string.Format(Strings.Container.StoppedSuccessFormat, service.Name));
            }
            else
            {
                AnsiConsole.MarkupLine(string.Format(Strings.Container.MayNotBeRunningFormat, service.Name));
            }
        }
    }

    /// <summary>
    /// View service logs (Docker or Native)
    /// </summary>
    public async Task ViewServiceLogsAsync(ServiceDefinition service)
    {
        var mode = service.GetExecutionMode();
        
        if (mode == ExecutionMode.Native)
        {
            // Native mode doesn't support log viewing
            AnsiConsole.MarkupLine("[yellow]Log viewing is not available for native mode services[/]");
            AnsiConsole.MarkupLine("[dim]Native services run in their own console windows or as background processes[/]");
        }
        else
        {
            var containerName = GetContainerName(service);
            await _dockerManager.DisplayContainerLogsAsync(containerName, 50, $"{service.Name} Logs");
        }
    }

    /// <summary>
    /// Show service parameters menu (CLI flags from services.yaml)
    /// Shows only parameters that have user-set values, plus an "Add" option for unset parameters
    /// </summary>
    public void ShowServiceParameters(ServiceDefinition service, Action showBanner)
    {
        if (service.Parameters == null || service.Parameters.Count == 0)
        {
            AnsiConsole.MarkupLine(Strings.ParametersMenu.NoParametersAvailable);
            return;
        }

        bool back = false;
        while (!back)
        {
            // Get parameters with user-set values (not hidden)
            var setParameters = service.Parameters
                .Where(p => !p.Hidden && p.HasUserValue)
                .ToList();

            // Build menu choices
            var choices = new List<string>();
            
            // Add set parameters with their values
            foreach (var param in setParameters)
            {
                var displayValue = FormatParameterValue(param);
                choices.Add($"{param.Name.PadRight(20)} {displayValue}");
            }
            
            // Add option to add new parameters
            var unsetParameters = service.Parameters
                .Where(p => !p.Hidden && !p.HasUserValue)
                .ToList();
            
            if (unsetParameters.Count > 0)
            {
                choices.Add(Strings.ParametersMenu.AddParameter);
            }
            
            choices.Add(Strings.ServiceMenu.Back);

            var title = setParameters.Count > 0 
                ? string.Format(Strings.ParametersMenu.TitleFormat, service.Name)
                : string.Format(Strings.ParametersMenu.TitleNoParamsFormat, service.Name);

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title(title)
                    .PageSize(15)
                    .AddChoices(choices)
            );

            if (choice == Strings.ServiceMenu.Back)
            {
                back = true;
                continue;
            }

            if (choice == Strings.ParametersMenu.AddParameter)
            {
                ShowAddParameterMenu(service, unsetParameters, showBanner);
                AnsiConsole.Clear();
                showBanner();
                continue;
            }

            // Find the selected parameter
            var selectedIndex = choices.IndexOf(choice);
            if (selectedIndex >= 0 && selectedIndex < setParameters.Count)
            {
                var param = setParameters[selectedIndex];
                EditParameter(service, param);
                
                AnsiConsole.Clear();
                showBanner();
            }
        }
    }

    /// <summary>
    /// Show service settings menu - different options based on execution mode
    /// </summary>
    public async Task ShowServiceSettings(ServiceDefinition service, Action showBanner)
    {
        bool back = false;
        while (!back)
        {
            var choices = new List<string>();
            var mode = service.GetExecutionMode();
            
            // First entry: Execution Mode (always shown)
            var executionModeValue = mode == ExecutionMode.Docker ? "[cyan]Docker[/]" : "[cyan]Native[/]";
            choices.Add($"{Strings.SettingsMenu.ExecutionMode.PadRight(20)} {executionModeValue}");
            
            // Second entry: Manage Versions (always shown with current version)
            var currentVersion = GetCurrentVersion(service, mode);
            var versionDisplay = !string.IsNullOrEmpty(currentVersion) 
                ? $" [cyan]{Markup.Escape(currentVersion)}[/]" 
                : "";
            choices.Add($"{Strings.SettingsMenu.ManageVersions.PadRight(20)}{versionDisplay}");
            
            if (mode == ExecutionMode.Docker)
            {
                // Docker mode settings in order: Container Name, Environment, Volumes, Ports, Network
                
                // Container Name
                var containerName = GetContainerName(service);
                choices.Add($"{Strings.SettingsMenu.ContainerName.PadRight(20)} [cyan]{Markup.Escape(containerName)}[/]");
                
                // Environment variables count
                var envCount = service.Environment?.Count ?? 0;
                choices.Add($"{Strings.SettingsMenu.Environment.PadRight(20)} [cyan]{envCount} configured[/]");
                
                // Volumes count
                var volumeCount = service.Volumes?.Count ?? 0;
                choices.Add($"{Strings.SettingsMenu.Volumes.PadRight(20)} [cyan]{volumeCount} configured[/]");
                
                // Ports count
                var portCount = service.Ports?.Count ?? 0;
                choices.Add($"{Strings.SettingsMenu.Ports.PadRight(20)} [cyan]{portCount} configured[/]");
                
                // Network
                var network = GetServiceNetwork(service);
                choices.Add($"{Strings.SettingsMenu.Network.PadRight(20)} [cyan]{Markup.Escape(network)}[/]");
            }
            else
            {
                // Native mode settings: Spawn in new process
                var spawnValue = service.SpawnNewConsole ? "[green]true[/]" : "[red]false[/]";
                choices.Add($"{Strings.SettingsMenu.SpawnNewConsole.PadRight(20)} {spawnValue}");
            }
            
            choices.Add(Strings.ServiceMenu.Back);

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title(string.Format(Strings.SettingsMenu.TitleFormat, service.Name))
                    .PageSize(15)
                    .AddChoices(choices)
            );

            if (choice == Strings.ServiceMenu.Back)
            {
                back = true;
                continue;
            }

            // Find which setting was selected
            var selectedIndex = choices.IndexOf(choice);
            
            if (selectedIndex == 0)
            {
                // Execution Mode
                EditExecutionMode(service);
            }
            else if (selectedIndex == 1)
            {
                // Manage Versions
                await ShowVersionManagementAsync(service, showBanner);
            }
            else if (mode == ExecutionMode.Docker)
            {
                // Docker mode settings (indices shifted by 2: execution mode + manage versions)
                switch (selectedIndex)
                {
                    case 2: // Container Name
                        EditServiceSetting(service, "container_name", Strings.SettingsMenu.ContainerName, showBanner);
                        break;
                    case 3: // Environment
                        ShowEnvironmentMenu(service, showBanner);
                        break;
                    case 4: // Volumes
                        ShowVolumesMenu(service, showBanner);
                        break;
                    case 5: // Ports
                        ShowPortsMenu(service, showBanner);
                        break;
                    case 6: // Network
                        EditServiceSetting(service, "network", Strings.SettingsMenu.Network, showBanner);
                        break;
                }
            }
            else
            {
                // Native mode settings (indices shifted by 2: execution mode + manage versions)
                switch (selectedIndex)
                {
                    case 2: // Spawn New Console
                        EditSpawnNewConsole(service);
                        break;
                }
            }
            
            AnsiConsole.Clear();
            showBanner();
        }
    }

    /// <summary>
    /// Edit execution mode setting
    /// </summary>
    private void EditExecutionMode(ServiceDefinition service)
    {
        var currentMode = service.GetExecutionMode();
        var options = new[] { "Docker", "Native" };
        
        var newMode = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"Select execution mode for [bold]{service.Name}[/]:\n[dim]Current: {(currentMode == ExecutionMode.Docker ? "Docker" : "Native")}[/]")
                .AddChoices(options)
                .HighlightStyle(new Style(Color.Green))
        );
        
        service.ExecutionModeString = newMode.ToLower();
        SaveServicesToYaml();
        
        AnsiConsole.MarkupLine(string.Format(Strings.SettingsMenu.SettingUpdatedFormat, Strings.SettingsMenu.ExecutionMode));
        AnsiConsole.MarkupLine($"[yellow]Note: You may need to download binaries (Native mode) or pull images (Docker mode) via 'Manage Versions'[/]");
    }

    /// <summary>
    /// Edit spawn new console setting for native mode
    /// </summary>
    private void EditSpawnNewConsole(ServiceDefinition service)
    {
        var currentValue = service.SpawnNewConsole;
        
        service.SpawnNewConsole = AnsiConsole.Confirm(
            $"Spawn {service.Name} in new console window?\n[dim](false = redirect output to log file)[/]",
            currentValue
        );
        
        SaveServicesToYaml();
        AnsiConsole.MarkupLine(string.Format(Strings.SettingsMenu.SettingUpdatedFormat, Strings.SettingsMenu.SpawnNewConsole));
    }

    /// <summary>
    /// Edit a service-level setting
    /// </summary>
    private void EditServiceSetting(ServiceDefinition service, string settingType, string settingName, Action showBanner)
    {
        string currentValue = settingType switch
        {
            "container_name" => GetContainerName(service),
            "network" => GetServiceNetwork(service),
            _ => ""
        };

        var newValue = AnsiConsole.Ask(string.Format(Strings.SettingsMenu.EnterValueFormat, settingName), currentValue);

        // Update the service configuration
        switch (settingType)
        {
            case "container_name":
                // Store as a custom setting value
                service.ContainerNameOverride = newValue;
                break;
            case "network":
                service.NetworkOverride = newValue;
                break;
        }

        SaveServicesToYaml();
        AnsiConsole.MarkupLine(string.Format(Strings.SettingsMenu.SettingUpdatedFormat, settingName));
    }

    /// <summary>
    /// Show volumes configuration menu
    /// </summary>
    private void ShowVolumesMenu(ServiceDefinition service, Action showBanner)
    {
        if (service.Volumes == null || service.Volumes.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No volumes configured for this service[/]");
            return;
        }

        var choices = new List<string>();
        foreach (var volume in service.Volumes)
        {
            var hostPath = GetVolumePath(service, volume);
            choices.Add($"{volume.Name.PadRight(15)} {Markup.Escape(hostPath ?? Strings.Status.NotSet)} -> {Markup.Escape(volume.ContainerPath)}");
        }
        choices.Add(Strings.ServiceMenu.Back);

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[bold green]{service.Name} Volumes[/]")
                .PageSize(15)
                .AddChoices(choices)
        );

        if (choice != Strings.ServiceMenu.Back)
        {
            var selectedIndex = choices.IndexOf(choice);
            if (selectedIndex >= 0 && selectedIndex < service.Volumes.Count)
            {
                var volume = service.Volumes[selectedIndex];
                var currentPath = GetVolumePath(service, volume) ?? "";
                var newPath = AnsiConsole.Ask($"Enter host path for {volume.Name}:", currentPath);
                volume.HostPathOverride = newPath;
                SaveServicesToYaml();
                AnsiConsole.MarkupLine(string.Format(Strings.SettingsMenu.SettingUpdatedFormat, volume.Name));
            }
        }
    }

    /// <summary>
    /// Show ports configuration menu
    /// </summary>
    private void ShowPortsMenu(ServiceDefinition service, Action showBanner)
    {
        if (service.Ports == null || service.Ports.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No ports configured for this service[/]");
            return;
        }

        var choices = new List<string>();
        foreach (var port in service.Ports)
        {
            var hostPort = GetPortValue(service, port);
            choices.Add($"{port.Name.PadRight(15)} {hostPort}:{port.ContainerPort}");
        }
        choices.Add(Strings.ServiceMenu.Back);

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[bold green]{service.Name} Ports[/]")
                .PageSize(15)
                .AddChoices(choices)
        );

        if (choice != Strings.ServiceMenu.Back)
        {
            var selectedIndex = choices.IndexOf(choice);
            if (selectedIndex >= 0 && selectedIndex < service.Ports.Count)
            {
                var port = service.Ports[selectedIndex];
                var currentPort = GetPortValue(service, port);
                var newPort = AnsiConsole.Ask($"Enter host port for {port.Name}:", currentPort);
                port.HostPortOverride = newPort;
                SaveServicesToYaml();
                AnsiConsole.MarkupLine(string.Format(Strings.SettingsMenu.SettingUpdatedFormat, port.Name));
            }
        }
    }

    /// <summary>
    /// Show environment variables configuration menu
    /// </summary>
    private void ShowEnvironmentMenu(ServiceDefinition service, Action showBanner)
    {
        bool back = false;
        while (!back)
        {
            var choices = new List<string>();
            var envVarCount = service.Environment?.Count ?? 0;
            
            // Add existing environment variables
            if (service.Environment != null)
            {
                foreach (var env in service.Environment)
                {
                    var currentValue = GetEnvironmentValue(env);
                    var displayValue = env.Sensitive ? "********" : Markup.Escape(currentValue);
                    choices.Add($"{env.Name.PadRight(20)} {displayValue}");
                }
            }
            
            // Add option to add new environment variable
            choices.Add(Strings.SettingsMenu.AddEnvironmentVar);
            choices.Add(Strings.ServiceMenu.Back);

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"[bold green]{service.Name} Environment Variables[/]")
                    .PageSize(15)
                    .AddChoices(choices)
            );

            if (choice == Strings.ServiceMenu.Back)
            {
                back = true;
                continue;
            }

            if (choice == Strings.SettingsMenu.AddEnvironmentVar)
            {
                // Add new environment variable
                var name = AnsiConsole.Ask<string>(Strings.SettingsMenu.EnterEnvVarName);
                if (!string.IsNullOrWhiteSpace(name))
                {
                    var value = AnsiConsole.Ask<string>(string.Format(Strings.SettingsMenu.EnterEnvVarValue, name));
                    
                    // Initialize Environment list if null
                    service.Environment ??= new List<EnvironmentVariable>();
                    
                    // Add the new environment variable
                    service.Environment.Add(new EnvironmentVariable
                    {
                        Name = name,
                        Value = value,
                        Description = Strings.SettingsMenu.UserDefinedEnvVarDescription
                    });
                    
                    SaveServicesToYaml();
                    AnsiConsole.MarkupLine(string.Format(Strings.SettingsMenu.EnvironmentVarAdded, name));
                }
            }
            else
            {
                // Edit existing environment variable - index is position in choices list
                // which matches position in Environment list since env vars are added first
                var envIndex = choices.IndexOf(choice);
                if (envIndex >= 0 && envIndex < envVarCount && service.Environment != null)
                {
                    var env = service.Environment[envIndex];
                    ShowEditEnvironmentVarMenu(service, env, showBanner);
                }
            }
            
            AnsiConsole.Clear();
            showBanner();
        }
    }

    /// <summary>
    /// Show menu to edit or remove an environment variable
    /// </summary>
    private void ShowEditEnvironmentVarMenu(ServiceDefinition service, EnvironmentVariable env, Action showBanner)
    {
        // Show description if available
        if (!string.IsNullOrEmpty(env.Description))
        {
            AnsiConsole.MarkupLine($"[dim]{Markup.Escape(env.Description)}[/]");
            AnsiConsole.WriteLine();
        }

        var currentValue = GetEnvironmentValue(env);
        var displayValue = env.Sensitive ? "********" : Markup.Escape(currentValue);

        var choices = new List<string>
        {
            Strings.ParametersMenu.EditValue,
            Strings.SettingsMenu.RemoveEnvironmentVar,
            Strings.ServiceMenu.Back
        };

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[bold]{env.Name}[/] = {displayValue}")
                .AddChoices(choices)
        );

        if (choice == Strings.ParametersMenu.EditValue)
        {
            var newValue = env.Sensitive 
                ? AnsiConsole.Prompt(new TextPrompt<string>(string.Format(Strings.SettingsMenu.EnterEnvVarValue, env.Name)).Secret())
                : AnsiConsole.Ask(string.Format(Strings.SettingsMenu.EnterEnvVarValue, env.Name), currentValue);
            
            env.ValueOverride = newValue;
            SaveServicesToYaml();
            AnsiConsole.MarkupLine(string.Format(Strings.SettingsMenu.SettingUpdatedFormat, env.Name));
        }
        else if (choice == Strings.SettingsMenu.RemoveEnvironmentVar)
        {
            service.Environment?.Remove(env);
            SaveServicesToYaml();
            AnsiConsole.MarkupLine(string.Format(Strings.SettingsMenu.EnvironmentVarRemoved, env.Name));
        }
    }

    /// <summary>
    /// Get environment variable value (from override or default)
    /// </summary>
    private string GetEnvironmentValue(EnvironmentVariable env)
    {
        return env.ValueOverride ?? env.Value ?? "";
    }

    /// <summary>
    /// Get service repository (from override or default)
    /// </summary>
    private string GetServiceRepository(ServiceDefinition service)
    {
        return service.Container?.Repository ?? "ghcr.io/ev1ls33d/pocx-wallet";
    }

    /// <summary>
    /// Get service tag (from override or default)
    /// </summary>
    private string GetServiceTag(ServiceDefinition service)
    {
        return service.Container?.DefaultTag ?? "latest";
    }

    /// <summary>
    /// Get service network (from override or default)
    /// </summary>
    private string GetServiceNetwork(ServiceDefinition service)
    {
        return service.NetworkOverride ?? _serviceConfig?.Defaults?.DockerNetwork ?? "pocx";
    }

    /// <summary>
    /// Get the current version for display in the settings menu
    /// </summary>
    private string GetCurrentVersion(ServiceDefinition service, ExecutionMode mode)
    {
        if (mode == ExecutionMode.Docker)
        {
            // For Docker, show the current tag
            return service.Container?.DefaultTag ?? "latest";
        }
        else
        {
            // For Native, try to determine installed version
            // This is a simple implementation - could be enhanced to read from version files
            var serviceDir = Path.Combine(".", service.Id);
            if (Directory.Exists(serviceDir))
            {
                // Check if there's a version indicator or just show "installed"
                return "installed";
            }
            return "not installed";
        }
    }

    /// <summary>
    /// Get volume path (from override or default in services.yaml)
    /// </summary>
    private string? GetVolumePath(ServiceDefinition service, VolumeMapping volume)
    {
        return volume.HostPathOverride ?? volume.HostPathDefault;
    }

    /// <summary>
    /// Get port value (from override or default in services.yaml)
    /// </summary>
    private int GetPortValue(ServiceDefinition service, PortMapping port)
    {
        return port.HostPortOverride ?? port.HostPortDefault ?? port.ContainerPort;
    }

    /// <summary>
    /// Format a parameter value for display
    /// </summary>
    private string FormatParameterValue(ServiceParameter param)
    {
        var value = param.Value;
        
        switch (param.Type.ToLower())
        {
            case "bool":
                var boolVal = value?.ToString()?.ToLower() == "true";
                return boolVal ? "[green]true[/]" : "[red]false[/]";
            
            case "int":
                return $"[cyan]{value}[/]";
            
            case "string[]":
                if (value is List<object> list && list.Count > 0)
                    return $"[cyan]{string.Join(", ", list)}[/]";
                return "[dim](empty)[/]";
            
            default:
                return $"[cyan]{Markup.Escape(value?.ToString() ?? "")}[/]";
        }
    }

    /// <summary>
    /// Show menu to add a new parameter
    /// </summary>
    private void ShowAddParameterMenu(ServiceDefinition service, List<ServiceParameter> unsetParameters, Action showBanner)
    {
        // Group by category for better organization
        var grouped = unsetParameters
            .GroupBy(p => p.Category ?? "general")
            .OrderBy(g => g.Key)
            .ToList();

        var choices = new List<string>();
        var paramMap = new Dictionary<string, ServiceParameter>();
        
        foreach (var group in grouped)
        {
            foreach (var param in group)
            {
                // Highlight required parameters with yellow color and asterisk
                var label = param.Required 
                    ? $"[dim]{group.Key}:[/] [yellow]{param.Name}*[/]"
                    : $"[dim]{group.Key}:[/] {param.Name}";
                choices.Add(label);
                paramMap[label] = param;
            }
        }
        
        choices.Add(Strings.ServiceMenu.Back);

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title(string.Format(Strings.ParametersMenu.AddParameterTitleFormat, service.Name))
                .PageSize(20)
                .AddChoices(choices)
        );

        if (choice == Strings.ServiceMenu.Back)
        {
            return;
        }

        if (paramMap.TryGetValue(choice, out var selectedParam))
        {
            // Show description before setting
            AnsiConsole.MarkupLine($"[dim]{Markup.Escape(selectedParam.Description)}[/]");
            AnsiConsole.WriteLine();
            
            // Set the parameter value
            SetParameterValue(service, selectedParam);
        }
    }

    /// <summary>
    /// Edit an existing parameter value (toggle for bool, or remove option)
    /// </summary>
    private void EditParameter(ServiceDefinition service, ServiceParameter param)
    {
        AnsiConsole.MarkupLine($"[dim]{Markup.Escape(param.Description)}[/]");
        AnsiConsole.WriteLine();

        var choices = new List<string>();
        
        if (param.Type.ToLower() == "bool")
        {
            choices.Add(Strings.ParametersMenu.ToggleValue);
        }
        else
        {
            choices.Add(Strings.ParametersMenu.EditValue);
        }
        choices.Add(Strings.ParametersMenu.RemoveParameter);
        choices.Add(Strings.ServiceMenu.Back);

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[bold]{param.Name}[/]: {FormatParameterValue(param)}")
                .AddChoices(choices)
        );

        if (choice == Strings.ParametersMenu.ToggleValue)
        {
            var currentBool = param.Value?.ToString()?.ToLower() == "true";
            param.Value = !currentBool;
            SaveServicesToYaml();
            AnsiConsole.MarkupLine(string.Format(Strings.ParametersMenu.ParameterSetFormat, param.Name, FormatParameterValue(param)));
        }
        else if (choice == Strings.ParametersMenu.EditValue)
        {
            SetParameterValue(service, param);
        }
        else if (choice == Strings.ParametersMenu.RemoveParameter)
        {
            param.Value = null;
            SaveServicesToYaml();
            AnsiConsole.MarkupLine(string.Format(Strings.ParametersMenu.ParameterRemovedFormat, param.Name));
        }
    }

    /// <summary>
    /// Set a parameter value based on its type
    /// </summary>
    private void SetParameterValue(ServiceDefinition service, ServiceParameter param)
    {
        switch (param.Type.ToLower())
        {
            case "bool":
                var defaultBool = param.Default?.ToString()?.ToLower() == "true";
                param.Value = AnsiConsole.Confirm(string.Format(Strings.Common.EnableFormat, param.Name), defaultBool);
                break;

            case "int":
                var defaultInt = 0;
                if (param.Default != null && int.TryParse(param.Default.ToString(), out var di))
                    defaultInt = di;
                
                while (true)
                {
                    var newInt = AnsiConsole.Ask(string.Format(Strings.Common.EnterFormat, param.Name), defaultInt);
                    
                    if (param.Validation != null)
                    {
                        if (param.Validation.Min.HasValue && newInt < param.Validation.Min.Value)
                        {
                            AnsiConsole.MarkupLine(string.Format(Strings.Validation.ValueMustBeAtLeastFormat, param.Validation.Min.Value));
                            continue;
                        }
                        if (param.Validation.Max.HasValue && newInt > param.Validation.Max.Value)
                        {
                            AnsiConsole.MarkupLine(string.Format(Strings.Validation.ValueMustBeAtMostFormat, param.Validation.Max.Value));
                            continue;
                        }
                    }
                    param.Value = newInt;
                    break;
                }
                break;

            case "string[]":
                var defaultArray = param.Default as List<object> ?? new List<object>();
                var input = AnsiConsole.Ask(string.Format(Strings.Common.EnterCommaSeparatedFormat, param.Name), string.Join(",", defaultArray));
                // Store as List<object> for consistent serialization
                param.Value = input.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => (object)s.Trim())
                    .ToList();
                break;

            case "string":
            default:
                var defaultStr = param.Default?.ToString() ?? "";
                
                // Handle enum values
                if (param.Enum != null && param.Enum.Count > 0)
                {
                    param.Value = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title(string.Format(Strings.Common.SelectFormat, param.Name))
                            .AddChoices(param.Enum)
                    );
                }
                else
                {
                    param.Value = AnsiConsole.Ask(string.Format(Strings.Common.EnterFormat, param.Name), defaultStr);
                }
                break;
        }

        SaveServicesToYaml();
        AnsiConsole.MarkupLine(string.Format(Strings.ParametersMenu.ParameterSetFormat, param.Name, FormatParameterValue(param)));
    }

    /// <summary>
    /// Save service configuration to services.yaml
    /// </summary>
    private void SaveServicesToYaml()
    {
        if (_serviceConfig != null)
        {
            ServiceDefinitionLoader.SaveServices(_serviceConfig);
        }
    }

    /// <summary>
    /// Handle custom actions defined in services.yaml.
    /// NOTE: Custom actions are placeholders for future implementation.
    /// The handler string in services.yaml specifies which method should be called,
    /// but actual implementation requires extending this method with reflection
    /// or a command registry pattern.
    /// </summary>
    private async Task HandleCustomActionAsync(ServiceDefinition service, SubmenuItem item)
    {
        // Check if this custom action has a command definition
        if (item.Command != null)
        {
            await ExecuteCustomCommandAsync(service, item.Command);
            return;
        }

        // Fallback for legacy custom actions without command definition
        AnsiConsole.MarkupLine(string.Format(Strings.CustomActions.NotImplementedFormat, item.Id));
        AnsiConsole.MarkupLine(Strings.CustomActions.RequiresImplementation);
        AnsiConsole.MarkupLine(string.Format(Strings.CustomActions.HandlerReferenceFormat, item.Handler));
        await Task.CompletedTask;
    }

    /// <summary>
    /// Execute a custom command defined in services.yaml
    /// Supports user inputs and macro placeholders
    /// </summary>
    private async Task ExecuteCustomCommandAsync(ServiceDefinition service, CustomCommand command)
    {
        var containerName = GetContainerName(service);

        // Show description if available
        if (!string.IsNullOrEmpty(command.Description))
        {
            AnsiConsole.MarkupLine($"[bold]{Markup.Escape(command.Description)}[/]");
            AnsiConsole.WriteLine();
        }

        try
        {
            // Collect user inputs
            var userInputs = _templateEngine.CollectUserInputs(command);

            // Process the command template
            var fullCommand = _templateEngine.ProcessCommand(command, userInputs);

            AnsiConsole.MarkupLine(string.Format(Strings.CustomActions.ExecutingFormat, Markup.Escape(fullCommand)));
            AnsiConsole.WriteLine();

            // Execute the command in the container
            var (exitCode, output) = await _dockerManager.ExecInContainerAsync(containerName, fullCommand);

            if (command.ShowOutput && !string.IsNullOrWhiteSpace(output))
            {
                AnsiConsole.MarkupLine(Strings.CustomActions.OutputHeader);
                AnsiConsole.WriteLine(output);
            }

            if (exitCode == 0)
            {
                AnsiConsole.MarkupLine(Strings.CustomActions.CommandSuccess);
            }
            else
            {
                AnsiConsole.MarkupLine(string.Format(Strings.CustomActions.CommandExitCodeFormat, exitCode));
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine(string.Format(Strings.CustomActions.CommandErrorFormat, Markup.Escape(ex.Message)));
        }
    }

    /// <summary>
    /// Check if GPU passthrough is enabled for a service.
    /// For the plotter, GPU passthrough is automatically enabled when the -g (gpu) parameter is set.
    /// </summary>
    private bool IsGpuPassthroughEnabled(ServiceDefinition service)
    {
        // For plotter service, check if the GPU parameter (-g) is set
        if (service.Id == "plotter" && service.Parameters != null)
        {
            var gpuParam = service.Parameters.FirstOrDefault(p => p.Name == "gpu" && p.CliFlag == "-g");
            if (gpuParam != null && gpuParam.HasUserValue)
            {
                // GPU parameter is set - check if it has a value (non-empty string array)
                if (gpuParam.Value is List<object> gpuList && gpuList.Count > 0)
                {
                    return true;
                }
                else if (gpuParam.Value is string gpuStr && !string.IsNullOrWhiteSpace(gpuStr))
                {
                    return true;
                }
            }
        }

        return false;
    }
    
    /// <summary>
    /// Show version management menu for downloading/pulling service versions
    /// </summary>
    private async Task ShowVersionManagementAsync(ServiceDefinition service, Action showBanner)
    {
        // Load GitHub token from wallet settings when entering version management
        var storedToken = WalletManager.Instance.Settings.GitHubToken;
        if (!string.IsNullOrWhiteSpace(storedToken))
        {
            _versionCrawler.SetGitHubToken(storedToken);
        }
        
        var mode = service.GetExecutionMode();
        
        if (mode == ExecutionMode.Native)
        {
            await ShowNativeVersionManagementAsync(service, showBanner);
        }
        else
        {
            await ShowDockerVersionManagementAsync(service, showBanner);
        }
    }
    
    /// <summary>
    /// Show Docker image version management
    /// </summary>
    private async Task ShowDockerVersionManagementAsync(ServiceDefinition service, Action showBanner)
    {
        var images = new List<DockerImage>();
        
        // Check for dynamic source first
        if (service.Source?.Docker?.Dynamic != null)
        {
            var dynamic = service.Source.Docker.Dynamic;
            AnsiConsole.MarkupLine("[dim]Discovering versions from dynamic source...[/]");
            
            var crawledImages = await _versionCrawler.CrawlContainerRegistryAsync(
                dynamic.Repository,
                dynamic.Filter
            );
            images.AddRange(crawledImages);
        }
        
        // Add static images (legacy support)
        if (service.Source?.Docker?.Images != null)
        {
            images.AddRange(service.Source.Docker.Images);
        }
        
        if (images.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No Docker images configured for this service[/]");
            AnsiConsole.MarkupLine("[dim]Add images in services.yaml under source.docker.images or source.docker.dynamic[/]");
            return;
        }
        
        // Build choices
        var choices = new List<string>();
        foreach (var image in images)
        {
            var desc = !string.IsNullOrEmpty(image.Description) ? $" - {image.Description}" : "";
            choices.Add($"{image.Tag}{desc}");
        }
        choices.Add("Back");
        
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[bold]Select Docker image to pull for {service.Name}:[/]")
                .PageSize(15)
                .AddChoices(choices)
        );
        
        if (choice == "Back")
        {
            return;
        }
        
        // Find selected image
        var selectedIndex = choices.IndexOf(choice);
        if (selectedIndex >= 0 && selectedIndex < images.Count)
        {
            var image = images[selectedIndex];
            var fullImageName = $"{image.Repository}/{image.Image}:{image.Tag}";
            
            AnsiConsole.MarkupLine($"[bold]Pulling Docker image:[/] {Markup.Escape(fullImageName)}");
            
            // Execute docker pull
            await AnsiConsole.Status()
                .StartAsync("Pulling image...", async ctx =>
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "docker",
                        Arguments = $"pull {fullImageName}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = new Process { StartInfo = psi };
                    process.Start();

                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    
                    await process.WaitForExitAsync();

                    if (process.ExitCode == 0)
                    {
                        ctx.Status("[green]Pull complete![/]");
                        AnsiConsole.MarkupLine("[green]√[/] Image pulled successfully");
                        
                        // Update service configuration to use this image
                        if (service.Container != null)
                        {
                            service.Container.Repository = image.Repository;
                            service.Container.Image = image.Image;
                            service.Container.DefaultTag = image.Tag;
                            SaveServicesToYaml();
                            AnsiConsole.MarkupLine("[green]√[/] Service configured to use this image");
                        }
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]Pull failed:[/] {Markup.Escape(error)}");
                    }
                });
            
            // Wait for user to read the feedback
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine(Strings.ServiceMenu.PressEnterToContinue);
            Console.ReadLine();
        }
    }
    
    /// <summary>
    /// Show native binary version management
    /// </summary>
    private async Task ShowNativeVersionManagementAsync(ServiceDefinition service, Action showBanner)
    {
        var downloads = new List<NativeDownload>();
        
        // Check for dynamic source first
        if (service.Source?.Native?.Dynamic != null)
        {
            var dynamic = service.Source.Native.Dynamic;
            AnsiConsole.MarkupLine("[dim]Discovering versions from dynamic source...[/]");
            
            var crawledDownloads = await _versionCrawler.CrawlGitHubReleasesAsync(
                dynamic.Repository,
                dynamic.Filter,
                null,
                dynamic.Whitelist
            );
            downloads.AddRange(crawledDownloads);
        }
        
        // Add static downloads (legacy support)
        if (service.Source?.Native?.Downloads != null)
        {
            downloads.AddRange(service.Source.Native.Downloads);
        }
        
        if (downloads.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No native downloads configured for this service[/]");
            AnsiConsole.MarkupLine("[dim]Add downloads in services.yaml under source.native.downloads or source.native.dynamic[/]");
            return;
        }
        
        // Filter by current platform
        var currentPlatform = NativeServiceManager.GetCurrentPlatform();
        var compatibleDownloads = downloads.Where(d => d.Platform == currentPlatform).ToList();
        
        if (compatibleDownloads.Count == 0)
        {
            AnsiConsole.MarkupLine($"[yellow]No downloads available for platform: {currentPlatform}[/]");
            AnsiConsole.MarkupLine($"[dim]Available platforms: {string.Join(", ", downloads.Select(d => d.Platform).Distinct())}[/]");
            return;
        }
        
        // Build choices
        var choices = new List<string>();
        foreach (var download in compatibleDownloads)
        {
            var desc = !string.IsNullOrEmpty(download.Description) ? $" - {download.Description}" : "";
            choices.Add($"{download.Version} ({download.Platform}){desc}");
        }
        choices.Add("Back");
        
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[bold]Select version to download for {service.Name}:[/]")
                .PageSize(15)
                .AddChoices(choices)
        );
        
        if (choice == "Back")
        {
            return;
        }
        
        // Find selected download
        var selectedIndex = choices.IndexOf(choice);
        if (selectedIndex >= 0 && selectedIndex < compatibleDownloads.Count)
        {
            var download = compatibleDownloads[selectedIndex];
            
            await _nativeManager.DownloadAndExtractNativeAsync(
                download.Url,
                download.Version,
                service.Id,
                service.Name,
                download.Whitelist
            );
            
            // Wait for user to read the feedback
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine(Strings.ServiceMenu.PressEnterToContinue);
            Console.ReadLine();
        }
    }
}

using PocxWallet.Cli.Services;
using Spectre.Console;

namespace PocxWallet.Cli.Configuration;

/// <summary>
/// Builds and manages dynamic menus from service definitions loaded from services.yaml
/// </summary>
public class DynamicServiceMenuBuilder
{
    private readonly DockerServiceManager _dockerManager;
    private readonly ServiceConfiguration? _serviceConfig;
    private readonly AppSettings _settings;

    public DynamicServiceMenuBuilder(ServiceConfiguration? serviceConfig, AppSettings settings, DockerServiceManager dockerManager)
    {
        _serviceConfig = serviceConfig;
        _settings = settings;
        _dockerManager = dockerManager;
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
        var containerName = GetContainerName(service);
        var status = await _dockerManager.GetContainerStatusAsync(containerName);
        return status == "running" ? "[green]●[/]" : "[red]●[/]";
    }

    /// <summary>
    /// Check if service is running
    /// </summary>
    public async Task<bool> IsServiceRunningAsync(ServiceDefinition service)
    {
        var containerName = GetContainerName(service);
        var status = await _dockerManager.GetContainerStatusAsync(containerName);
        return status == "running";
    }

    /// <summary>
    /// Get the container name for a service
    /// </summary>
    public string GetContainerName(ServiceDefinition service)
    {
        // Try to resolve from container_name_setting if it references AppSettings
        if (service.Container?.ContainerNameSetting != null)
        {
            var settingName = service.Container.ContainerNameSetting;
            var containerName = GetSettingValue(settingName);
            if (!string.IsNullOrEmpty(containerName))
            {
                return containerName;
            }
        }
        
        // Fallback to default naming convention
        return $"pocx-{service.Id}";
    }

    /// <summary>
    /// Get repository URL for a service
    /// </summary>
    public string GetRepository(ServiceDefinition service)
    {
        return service.Container?.Repository ?? "ghcr.io/ev1ls33d/pocx-wallet";
    }

    /// <summary>
    /// Get image tag for a service
    /// </summary>
    public string GetImageTag(ServiceDefinition service)
    {
        return service.Container?.DefaultTag ?? "latest";
    }

    /// <summary>
    /// Get image name for a service
    /// </summary>
    public string GetImageName(ServiceDefinition service)
    {
        return service.Container?.Image ?? service.Id;
    }

    /// <summary>
    /// Build port mappings dictionary for Docker
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

            var hostPort = GetPortFromSetting(port.HostPortSetting) ?? port.ContainerPort;
            mappings[hostPort] = port.ContainerPort;
        }

        return mappings;
    }

    /// <summary>
    /// Build volume mappings dictionary for Docker
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
            var hostPath = GetPathFromSetting(volume.HostPathSetting);
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
            var hostPath = GetPathFromSetting(volume.HostPathSetting);
            if (!string.IsNullOrEmpty(hostPath))
            {
                readOnlyPaths.Add(hostPath);
            }
        }

        return readOnlyPaths;
    }

    /// <summary>
    /// Build command string from parameters
    /// </summary>
    public string? BuildCommand(ServiceDefinition service)
    {
        var commands = new List<string>();
        
        // Add the base command if specified
        if (!string.IsNullOrEmpty(service.Container?.Command))
        {
            commands.Add(service.Container.Command);
        }

        // Add default parameters
        if (service.Parameters != null)
        {
            foreach (var param in service.Parameters.Where(p => !p.Hidden && p.Default != null))
            {
                if (param.Type == "bool" && param.Default is bool boolValue && boolValue && !string.IsNullOrEmpty(param.CliFlag))
                {
                    commands.Add(param.CliFlag);
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
            var statusIndicator = isRunning ? "[green]●[/]" : "[red]●[/]";
            
            // Build dynamic submenu from service definition
            var choices = new List<string>();
            
            if (service.Menu?.Submenu != null)
            {
                foreach (var item in service.Menu.Submenu)
                {
                    var label = item.Action switch
                    {
                        "toggle" => isRunning ? (item.LabelRunning ?? "Stop Service") : (item.LabelStopped ?? "Start Service"),
                        "logs" => item.Label ?? "View Logs",
                        "settings" => item.Label ?? "Settings",
                        "custom" => item.Label ?? item.Id ?? "Custom Action",
                        _ => item.Label ?? item.Action
                    };
                    choices.Add(label);
                }
            }
            else
            {
                // Default menu if not defined
                choices.Add(isRunning ? "Stop Service" : "Start Service");
                choices.Add("View Logs");
            }
            
            choices.Add("<= Back");

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"[bold green]{service.Name} - Service: {statusIndicator}[/]")
                    .PageSize(10)
                    .AddChoices(choices)
            );

            AnsiConsole.Clear();
            showBanner();

            if (choice == "<= Back")
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

            if (!back && selectedItem?.Action != "settings")
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[dim]Press ENTER to continue...[/]");
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
            if (choice == "View Logs")
            {
                return new SubmenuItem { Action = "logs" };
            }
            return null;
        }

        foreach (var item in service.Menu.Submenu)
        {
            var label = item.Action switch
            {
                "toggle" => isRunning ? (item.LabelRunning ?? "Stop Service") : (item.LabelStopped ?? "Start Service"),
                "logs" => item.Label ?? "View Logs",
                "settings" => item.Label ?? "Settings",
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

            case "settings":
                ShowServiceSettings(service, showBanner);
                break;

            case "custom":
                await HandleCustomActionAsync(service, item);
                break;
        }
    }

    /// <summary>
    /// Start a service container
    /// </summary>
    public async Task StartServiceAsync(ServiceDefinition service)
    {
        var containerName = GetContainerName(service);
        var imageName = GetImageName(service);
        var repository = GetRepository(service);
        var tag = GetImageTag(service);
        var network = _serviceConfig?.Defaults?.DockerNetwork ?? _settings.DockerNetwork;

        AnsiConsole.MarkupLine($"[bold]Starting {service.Name}...[/]");

        // Ensure network exists
        await _dockerManager.EnsureNetworkExistsAsync(network);

        // Build volume mappings
        var volumes = BuildVolumeMappings(service);
        
        // Ensure volume directories/files exist
        if (service.Volumes != null)
        {
            foreach (var volume in service.Volumes)
            {
                var hostPath = GetPathFromSetting(volume.HostPathSetting);
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
                                AnsiConsole.MarkupLine($"[dim]Created directory: {Markup.Escape(parentDir)}[/]");
                            }
                        }
                        else
                        {
                            Directory.CreateDirectory(hostPath);
                            AnsiConsole.MarkupLine($"[dim]Created directory: {Markup.Escape(hostPath)}[/]");
                        }
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[yellow]Warning: Could not create path {Markup.Escape(hostPath)}: {Markup.Escape(ex.Message)}[/]");
                    }
                }
            }
        }

        var ports = BuildPortMappings(service);
        var readOnlyVolumes = GetReadOnlyVolumes(service);
        var command = BuildCommand(service);

        var success = await _dockerManager.StartContainerAsync(
            containerName,
            imageName,
            repository,
            tag,
            environmentVars: null,
            volumeMounts: volumes.Count > 0 ? volumes : null,
            portMappings: ports.Count > 0 ? ports : null,
            command: command,
            network: network,
            readOnlyVolumes: readOnlyVolumes.Count > 0 ? readOnlyVolumes : null
        );

        if (success)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] {service.Name} started successfully");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]✗[/] Failed to start {service.Name}");
        }
    }

    /// <summary>
    /// Stop a service container
    /// </summary>
    public async Task StopServiceAsync(ServiceDefinition service)
    {
        var containerName = GetContainerName(service);
        AnsiConsole.MarkupLine($"[bold]Stopping {service.Name}...[/]");

        var success = await _dockerManager.StopContainerAsync(containerName);

        if (success)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] {service.Name} stopped successfully");
        }
        else
        {
            AnsiConsole.MarkupLine($"[yellow]⚠[/] {service.Name} may not have been running");
        }
    }

    /// <summary>
    /// View service logs
    /// </summary>
    public async Task ViewServiceLogsAsync(ServiceDefinition service)
    {
        var containerName = GetContainerName(service);
        await _dockerManager.DisplayContainerLogsAsync(containerName, 50, $"{service.Name} Logs");
    }

    /// <summary>
    /// Show service settings menu with inline editing
    /// </summary>
    public void ShowServiceSettings(ServiceDefinition service, Action showBanner)
    {
        if (service.Settings == null || service.Settings.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No settings available for this service[/]");
            return;
        }

        bool back = false;
        while (!back)
        {
            // Build menu choices with current values
            var choices = new List<string>();
            foreach (var setting in service.Settings)
            {
                var currentValue = GetSettingValue(setting.SettingPath) ?? "(not set)";
                var displayValue = setting.Type == "bool" 
                    ? (currentValue.ToLower() == "true" ? "[green]On[/]" : "[red]Off[/]")
                    : $"[dim]{Markup.Escape(currentValue)}[/]";
                choices.Add($"{setting.Key.PadRight(20)} {displayValue}");
            }
            choices.Add("<= Back");

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"[bold green]{service.Name} Settings[/]")
                    .PageSize(15)
                    .AddChoices(choices)
            );

            if (choice == "<= Back")
            {
                back = true;
                continue;
            }

            // Find the selected setting
            var selectedIndex = choices.IndexOf(choice);
            if (selectedIndex >= 0 && selectedIndex < service.Settings.Count)
            {
                var setting = service.Settings[selectedIndex];
                EditSetting(setting);
                
                AnsiConsole.Clear();
                showBanner();
            }
        }
    }

    /// <summary>
    /// Edit a single setting value
    /// </summary>
    private void EditSetting(ServiceSetting setting)
    {
        var currentValue = GetSettingValue(setting.SettingPath);

        switch (setting.Type.ToLower())
        {
            case "bool":
                // Toggle boolean value
                var currentBool = currentValue?.ToLower() == "true";
                var newBool = !currentBool;
                SetSettingValue(setting.SettingPath, newBool.ToString());
                AnsiConsole.MarkupLine($"[green]✓[/] {setting.Key} set to {(newBool ? "[green]On[/]" : "[red]Off[/]")}");
                break;

            case "int":
                var currentInt = int.TryParse(currentValue, out var intVal) ? intVal : 0;
                var newInt = AnsiConsole.Ask($"Enter {setting.Key}:", currentInt);
                
                // Validate if min/max specified
                if (setting.Validation != null)
                {
                    if (setting.Validation.Min.HasValue && newInt < setting.Validation.Min.Value)
                    {
                        AnsiConsole.MarkupLine($"[yellow]Value must be at least {setting.Validation.Min.Value}[/]");
                        return;
                    }
                    if (setting.Validation.Max.HasValue && newInt > setting.Validation.Max.Value)
                    {
                        AnsiConsole.MarkupLine($"[yellow]Value must be at most {setting.Validation.Max.Value}[/]");
                        return;
                    }
                }
                
                SetSettingValue(setting.SettingPath, newInt.ToString());
                AnsiConsole.MarkupLine($"[green]✓[/] {setting.Key} updated");
                break;

            case "path":
            case "string":
            default:
                var newString = AnsiConsole.Ask($"Enter {setting.Key}:", currentValue ?? "");
                SetSettingValue(setting.SettingPath, newString);
                AnsiConsole.MarkupLine($"[green]✓[/] {setting.Key} updated");
                break;
        }

        // Save settings to appsettings.json
        SaveSettings();
    }

    /// <summary>
    /// Set a setting value by path
    /// </summary>
    private void SetSettingValue(string settingPath, string value)
    {
        switch (settingPath)
        {
            // Root-level settings
            case "PlotDirectory":
                _settings.PlotDirectory = value;
                break;
            case "MinerConfigPath":
                _settings.MinerConfigPath = value;
                break;
            case "WalletFilePath":
                _settings.WalletFilePath = value;
                break;
            case "DockerNetwork":
                _settings.DockerNetwork = value;
                break;
            case "BitcoinContainerName":
                _settings.BitcoinContainerName = value;
                break;
            case "MinerContainerName":
                _settings.MinerContainerName = value;
                break;
            case "PlotterContainerName":
                _settings.PlotterContainerName = value;
                break;
            case "ElectrsContainerName":
                _settings.ElectrsContainerName = value;
                break;

            // Bitcoin Node settings
            case "BitcoinNode.Repository":
                _settings.BitcoinNode.Repository = value;
                break;
            case "BitcoinNode.Tag":
                _settings.BitcoinNode.Tag = value;
                break;
            case "BitcoinNode.RpcPort":
                if (int.TryParse(value, out var rpcPort))
                    _settings.BitcoinNode.RpcPort = rpcPort;
                break;
            case "BitcoinNode.P2PPort":
                if (int.TryParse(value, out var p2pPort))
                    _settings.BitcoinNode.P2PPort = p2pPort;
                break;
            case "BitcoinNode.DataDirectory":
                _settings.BitcoinNode.DataDirectory = value;
                break;
            case "BitcoinNode.AdditionalParams":
                _settings.BitcoinNode.AdditionalParams = value;
                break;

            // Electrs settings
            case "Electrs.Repository":
                _settings.Electrs.Repository = value;
                break;
            case "Electrs.Tag":
                _settings.Electrs.Tag = value;
                break;
            case "Electrs.HttpPort":
                if (int.TryParse(value, out var httpPort))
                    _settings.Electrs.HttpPort = httpPort;
                break;
            case "Electrs.RpcPort":
                if (int.TryParse(value, out var electrsRpcPort))
                    _settings.Electrs.RpcPort = electrsRpcPort;
                break;
            case "Electrs.TestnetPort":
                if (int.TryParse(value, out var testnetPort))
                    _settings.Electrs.TestnetPort = testnetPort;
                break;
            case "Electrs.DataDirectory":
                _settings.Electrs.DataDirectory = value;
                break;
            case "Electrs.AdditionalParams":
                _settings.Electrs.AdditionalParams = value;
                break;

            // Miner settings
            case "Miner.Repository":
                _settings.Miner.Repository = value;
                break;
            case "Miner.Tag":
                _settings.Miner.Tag = value;
                break;
            case "Miner.CpuThreads":
                if (int.TryParse(value, out var cpuThreads))
                    _settings.Miner.CpuThreads = cpuThreads;
                break;
            case "Miner.UseDirectIO":
                _settings.Miner.UseDirectIO = value.ToLower() == "true";
                break;
            case "Miner.ShowProgress":
                _settings.Miner.ShowProgress = value.ToLower() == "true";
                break;
            case "Miner.AdditionalParams":
                _settings.Miner.AdditionalParams = value;
                break;

            // Plotter settings
            case "Plotter.Repository":
                _settings.Plotter.Repository = value;
                break;
            case "Plotter.Tag":
                _settings.Plotter.Tag = value;
                break;
            case "Plotter.DefaultWarps":
                if (int.TryParse(value, out var warps))
                    _settings.Plotter.DefaultWarps = warps;
                break;
            case "Plotter.CpuThreads":
                if (int.TryParse(value, out var plotterCpuThreads))
                    _settings.Plotter.CpuThreads = plotterCpuThreads;
                break;
            case "Plotter.UseDirectIO":
                _settings.Plotter.UseDirectIO = value.ToLower() == "true";
                break;
            case "Plotter.AdditionalParams":
                _settings.Plotter.AdditionalParams = value;
                break;
        }
    }

    /// <summary>
    /// Save settings to appsettings.json
    /// </summary>
    private void SaveSettings()
    {
        SettingsManager.SaveSettings(_settings);
        AnsiConsole.MarkupLine("[dim]Settings saved to appsettings.json[/]");
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
        AnsiConsole.MarkupLine($"[yellow]Custom action '{item.Id}' is not yet implemented[/]");
        AnsiConsole.MarkupLine($"[dim]This action is defined in services.yaml but requires code implementation.[/]");
        AnsiConsole.MarkupLine($"[dim]Handler reference: {item.Handler}[/]");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Get a setting value from AppSettings by path
    /// </summary>
    private string? GetSettingValue(string? settingPath)
    {
        if (string.IsNullOrEmpty(settingPath))
        {
            return null;
        }

        // Handle common setting paths
        return settingPath switch
        {
            "BitcoinContainerName" => _settings.BitcoinContainerName,
            "ElectrsContainerName" => _settings.ElectrsContainerName,
            "MinerContainerName" => _settings.MinerContainerName,
            "PlotterContainerName" => _settings.PlotterContainerName,
            "BitcoinNode.Repository" => _settings.BitcoinNode.Repository,
            "BitcoinNode.Tag" => _settings.BitcoinNode.Tag,
            "BitcoinNode.DataDirectory" => _settings.BitcoinNode.DataDirectory,
            "BitcoinNode.RpcPort" => _settings.BitcoinNode.RpcPort.ToString(),
            "BitcoinNode.P2PPort" => _settings.BitcoinNode.P2PPort.ToString(),
            "Electrs.Repository" => _settings.Electrs.Repository,
            "Electrs.Tag" => _settings.Electrs.Tag,
            "Electrs.DataDirectory" => _settings.Electrs.DataDirectory,
            "Electrs.HttpPort" => _settings.Electrs.HttpPort.ToString(),
            "Electrs.RpcPort" => _settings.Electrs.RpcPort.ToString(),
            "Electrs.TestnetPort" => _settings.Electrs.TestnetPort.ToString(),
            "Miner.Repository" => _settings.Miner.Repository,
            "Miner.Tag" => _settings.Miner.Tag,
            "Plotter.Repository" => _settings.Plotter.Repository,
            "Plotter.Tag" => _settings.Plotter.Tag,
            "PlotDirectory" => _settings.PlotDirectory,
            "MinerConfigPath" => _settings.MinerConfigPath,
            _ => null
        };
    }

    /// <summary>
    /// Get port number from setting path
    /// </summary>
    private int? GetPortFromSetting(string? settingPath)
    {
        var value = GetSettingValue(settingPath);
        if (int.TryParse(value, out var port))
        {
            return port;
        }
        return null;
    }

    /// <summary>
    /// Get path from setting path
    /// </summary>
    private string? GetPathFromSetting(string? settingPath)
    {
        return GetSettingValue(settingPath);
    }
}

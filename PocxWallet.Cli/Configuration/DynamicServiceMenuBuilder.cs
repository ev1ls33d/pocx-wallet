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
    /// Show service settings menu based on parameters from services.yaml
    /// Shows only parameters that have user-set values, plus an "Add" option for unset parameters
    /// </summary>
    public void ShowServiceSettings(ServiceDefinition service, Action showBanner)
    {
        if (service.Parameters == null || service.Parameters.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No parameters available for this service[/]");
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
                choices.Add("[Add Parameter]");
            }
            
            choices.Add("<= Back");

            var title = setParameters.Count > 0 
                ? $"[bold green]{service.Name} Settings[/]"
                : $"[bold green]{service.Name} Settings[/] [dim](no parameters set)[/]";

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title(title)
                    .PageSize(15)
                    .AddChoices(choices)
            );

            if (choice == "<= Back")
            {
                back = true;
                continue;
            }

            if (choice == "[Add Parameter]")
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
                if (value is List<object> list)
                    return $"[cyan][{string.Join(", ", list)}][/]";
                return $"[cyan]{value}[/]";
            
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
                var label = $"[dim]{group.Key}:[/] {param.Name}";
                choices.Add(label);
                paramMap[label] = param;
            }
        }
        
        choices.Add("<= Back");

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[bold green]Add Parameter to {service.Name}[/]")
                .PageSize(20)
                .AddChoices(choices)
        );

        if (choice == "<= Back")
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
            choices.Add("Toggle Value");
        }
        else
        {
            choices.Add("Edit Value");
        }
        choices.Add("[red]Remove Parameter[/]");
        choices.Add("<= Back");

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[bold]{param.Name}[/]: {FormatParameterValue(param)}")
                .AddChoices(choices)
        );

        switch (choice)
        {
            case "Toggle Value":
                var currentBool = param.Value?.ToString()?.ToLower() == "true";
                param.Value = !currentBool;
                SaveServicesToYaml();
                AnsiConsole.MarkupLine($"[green]✓[/] {param.Name} set to {FormatParameterValue(param)}");
                break;
            
            case "Edit Value":
                SetParameterValue(service, param);
                break;
            
            case "[red]Remove Parameter[/]":
                param.Value = null;
                SaveServicesToYaml();
                AnsiConsole.MarkupLine($"[green]✓[/] {param.Name} removed");
                break;
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
                param.Value = AnsiConsole.Confirm($"Enable {param.Name}?", defaultBool);
                break;

            case "int":
                var defaultInt = 0;
                if (param.Default != null && int.TryParse(param.Default.ToString(), out var di))
                    defaultInt = di;
                
                while (true)
                {
                    var newInt = AnsiConsole.Ask($"Enter {param.Name}:", defaultInt);
                    
                    if (param.Validation != null)
                    {
                        if (param.Validation.Min.HasValue && newInt < param.Validation.Min.Value)
                        {
                            AnsiConsole.MarkupLine($"[yellow]Value must be at least {param.Validation.Min.Value}. Please try again.[/]");
                            continue;
                        }
                        if (param.Validation.Max.HasValue && newInt > param.Validation.Max.Value)
                        {
                            AnsiConsole.MarkupLine($"[yellow]Value must be at most {param.Validation.Max.Value}. Please try again.[/]");
                            continue;
                        }
                    }
                    param.Value = newInt;
                    break;
                }
                break;

            case "string[]":
                var defaultArray = param.Default as List<object> ?? new List<object>();
                var input = AnsiConsole.Ask($"Enter {param.Name} (comma-separated):", string.Join(",", defaultArray));
                param.Value = input.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
                break;

            case "string":
            default:
                var defaultStr = param.Default?.ToString() ?? "";
                
                // Handle enum values
                if (param.Enum != null && param.Enum.Count > 0)
                {
                    param.Value = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title($"Select {param.Name}:")
                            .AddChoices(param.Enum)
                    );
                }
                else
                {
                    param.Value = AnsiConsole.Ask($"Enter {param.Name}:", defaultStr);
                }
                break;
        }

        SaveServicesToYaml();
        AnsiConsole.MarkupLine($"[green]✓[/] {param.Name} set to {FormatParameterValue(param)}");
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

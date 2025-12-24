using PocxWallet.Cli.Resources;
using PocxWallet.Cli.Services;
using PocxWallet.Core.Wallet;
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
    private readonly CommandTemplateEngine _templateEngine;
    private readonly Func<HDWallet?> _walletProvider;

    public DynamicServiceMenuBuilder(
        ServiceConfiguration? serviceConfig, 
        AppSettings settings, 
        DockerServiceManager dockerManager,
        Func<HDWallet?>? walletProvider = null)
    {
        _serviceConfig = serviceConfig;
        _settings = settings;
        _dockerManager = dockerManager;
        _walletProvider = walletProvider ?? (() => null);
        _templateEngine = new CommandTemplateEngine(_walletProvider);
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
    /// Get the container name for a service (uses override if set)
    /// </summary>
    public string GetContainerName(ServiceDefinition service)
    {
        // First check for user override
        if (!string.IsNullOrEmpty(service.ContainerNameOverride))
        {
            return service.ContainerNameOverride;
        }

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
                var useEquals = param.UseEquals ?? (param.Type.ToLower() != "bool");

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
            
            if (service.Menu?.Submenu != null)
            {
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
                    choices.Add(label);
                }
            }
            else
            {
                // Default menu if not defined
                choices.Add(isRunning ? Strings.ServiceMenu.StopService : Strings.ServiceMenu.StartService);
                choices.Add(Strings.ServiceMenu.ViewLogs);
                choices.Add(Strings.ServiceMenu.Parameters);
                choices.Add(Strings.ServiceMenu.Settings);
            }
            
            choices.Add(Strings.ServiceMenu.Back);

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title(string.Format(Strings.ServiceMenu.TitleFormat, service.Name, statusIndicator))
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

            case "parameters":
                ShowServiceParameters(service, showBanner);
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
    /// Start a service container using settings from services.yaml
    /// </summary>
    public async Task StartServiceAsync(ServiceDefinition service)
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
            AnsiConsole.MarkupLine(string.Format(Strings.Container.StartedSuccessFormat, service.Name));
        }
        else
        {
            AnsiConsole.MarkupLine(string.Format(Strings.Container.StartFailedFormat, service.Name));
        }
    }

    /// <summary>
    /// Stop a service container
    /// </summary>
    public async Task StopServiceAsync(ServiceDefinition service)
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

    /// <summary>
    /// View service logs
    /// </summary>
    public async Task ViewServiceLogsAsync(ServiceDefinition service)
    {
        var containerName = GetContainerName(service);
        await _dockerManager.DisplayContainerLogsAsync(containerName, 50, $"{service.Name} Logs");
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
    /// Show service settings menu (Docker service-level: repository, tag, volumes, ports, etc.)
    /// </summary>
    public void ShowServiceSettings(ServiceDefinition service, Action showBanner)
    {
        bool back = false;
        while (!back)
        {
            var choices = new List<string>();
            
            // Repository
            var repo = GetServiceRepository(service);
            choices.Add($"{Strings.SettingsMenu.Repository.PadRight(20)} [cyan]{Markup.Escape(repo)}[/]");
            
            // Tag
            var tag = GetServiceTag(service);
            choices.Add($"{Strings.SettingsMenu.Tag.PadRight(20)} [cyan]{Markup.Escape(tag)}[/]");
            
            // Container Name
            var containerName = GetContainerName(service);
            choices.Add($"{Strings.SettingsMenu.ContainerName.PadRight(20)} [cyan]{Markup.Escape(containerName)}[/]");
            
            // Network
            var network = GetServiceNetwork(service);
            choices.Add($"{Strings.SettingsMenu.Network.PadRight(20)} [cyan]{Markup.Escape(network)}[/]");
            
            // Volumes count
            var volumeCount = service.Volumes?.Count ?? 0;
            choices.Add($"{Strings.SettingsMenu.Volumes.PadRight(20)} [cyan]{volumeCount} configured[/]");
            
            // Ports count
            var portCount = service.Ports?.Count ?? 0;
            choices.Add($"{Strings.SettingsMenu.Ports.PadRight(20)} [cyan]{portCount} configured[/]");
            
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
            switch (selectedIndex)
            {
                case 0: // Repository
                    EditServiceSetting(service, "repository", Strings.SettingsMenu.Repository, showBanner);
                    break;
                case 1: // Tag
                    EditServiceSetting(service, "tag", Strings.SettingsMenu.Tag, showBanner);
                    break;
                case 2: // Container Name
                    EditServiceSetting(service, "container_name", Strings.SettingsMenu.ContainerName, showBanner);
                    break;
                case 3: // Network
                    EditServiceSetting(service, "network", Strings.SettingsMenu.Network, showBanner);
                    break;
                case 4: // Volumes
                    ShowVolumesMenu(service, showBanner);
                    break;
                case 5: // Ports
                    ShowPortsMenu(service, showBanner);
                    break;
            }
            
            AnsiConsole.Clear();
            showBanner();
        }
    }

    /// <summary>
    /// Edit a service-level setting
    /// </summary>
    private void EditServiceSetting(ServiceDefinition service, string settingType, string settingName, Action showBanner)
    {
        string currentValue = settingType switch
        {
            "repository" => GetServiceRepository(service),
            "tag" => GetServiceTag(service),
            "container_name" => GetContainerName(service),
            "network" => GetServiceNetwork(service),
            _ => ""
        };

        var newValue = AnsiConsole.Ask(string.Format(Strings.SettingsMenu.EnterValueFormat, settingName), currentValue);

        // Update the service configuration
        switch (settingType)
        {
            case "repository":
                if (service.Container != null)
                    service.Container.Repository = newValue;
                break;
            case "tag":
                if (service.Container != null)
                    service.Container.DefaultTag = newValue;
                break;
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
        return service.NetworkOverride ?? _serviceConfig?.Defaults?.DockerNetwork ?? _settings.DockerNetwork;
    }

    /// <summary>
    /// Get volume path (from override or setting)
    /// </summary>
    private string? GetVolumePath(ServiceDefinition service, VolumeMapping volume)
    {
        return volume.HostPathOverride ?? GetPathFromSetting(volume.HostPathSetting);
    }

    /// <summary>
    /// Get port value (from override or setting)
    /// </summary>
    private int GetPortValue(ServiceDefinition service, PortMapping port)
    {
        return port.HostPortOverride ?? GetPortFromSetting(port.HostPortSetting) ?? port.ContainerPort;
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
                var label = $"[dim]{group.Key}:[/] {param.Name}";
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

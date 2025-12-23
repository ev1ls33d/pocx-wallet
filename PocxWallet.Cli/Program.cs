using System;
using System.Linq;
using System.Threading.Tasks;
using PocxWallet.Cli.Commands;
using PocxWallet.Cli.Configuration;
using PocxWallet.Cli.Services;
using Spectre.Console;

namespace PocxWallet.Cli;

// Legacy enum - kept for potential future use but main menu now uses string-based dynamic choices
enum MenuOptions
{
    // Wallet submenu
    Wallet_CreateNewWallet,
    Wallet_RestoreWalletFromMnemonic,
    Wallet_ShowAddresses,
    Wallet_CheckBalance,
    Wallet_SendFunds,
    Wallet_TransactionHistory,
    
    // General back option
    General_Back
}

static class MenuOptionsExtensions
{
    public static string ToDisplayString(this MenuOptions option) =>
        option switch
        {
            // Wallet
            MenuOptions.Wallet_CreateNewWallet =>               Markup.Escape("Create New Wallet"),
            MenuOptions.Wallet_RestoreWalletFromMnemonic =>     Markup.Escape("Restore Wallet from Mnemonic"),
            MenuOptions.Wallet_ShowAddresses =>                 Markup.Escape("Show Addresses"),
            MenuOptions.Wallet_CheckBalance =>                  Markup.Escape("Check Balance"),
            MenuOptions.Wallet_SendFunds =>                     Markup.Escape("Send Funds"),
            MenuOptions.Wallet_TransactionHistory =>            Markup.Escape("Transaction History"),
            MenuOptions.General_Back =>                         Markup.Escape("<= Back"),
            _ => Markup.Escape(option.ToString())
        };
}

class Program
{
    private static readonly AppSettings _settings = new();
    private static DockerServiceManager? _dockerManager;
    private static ServiceConfiguration? _serviceConfig;
    private static DynamicServiceMenuBuilder? _dynamicMenuBuilder;

    // Constants for hardcoded menu items
    private const string MenuWallet = "[Wallet]    Wallet Management";
    private const string MenuVanity = "[Vanity]    Vanity Address Generator";
    private const string MenuExit = "[Exit]      Exit";

    private static DockerServiceManager GetDockerManager()
    {
        if (_dockerManager == null)
        {
            _dockerManager = new DockerServiceManager();
        }
        return _dockerManager;
    }

    private static DynamicServiceMenuBuilder GetDynamicMenuBuilder()
    {
        if (_dynamicMenuBuilder == null)
        {
            _dynamicMenuBuilder = new DynamicServiceMenuBuilder(_serviceConfig, _settings, GetDockerManager());
        }
        return _dynamicMenuBuilder;
    }

    /// <summary>
    /// Get service status as colored LED indicator
    /// </summary>
    private static async Task<string> GetServiceStatusIndicatorAsync(string containerName)
    {
        var docker = GetDockerManager();
        var status = await docker.GetContainerStatusAsync(containerName);
        return status == "running" ? "[green]●[/]" : "[red]●[/]";
    }

    /// <summary>
    /// Check if service is running
    /// </summary>
    private static async Task<bool> IsServiceRunningAsync(string containerName)
    {
        var docker = GetDockerManager();
        var status = await docker.GetContainerStatusAsync(containerName);
        return status == "running";
    }

    /// <summary>
    /// Get menu display string with status indicator
    /// </summary>
    private static async Task<string> GetMenuItemWithStatusAsync(string label, string containerName)
    {
        var status = await GetServiceStatusIndicatorAsync(containerName);
        return $"{label} {status}";
    }

    static async Task Main(string[] args)
    {
        // Set UTF-8 encoding for better compatibility
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;

        // Check if demo mode is requested
        if (args.Length > 0 && args[0] == "--demo")
        {
            Demo.RunWalletDemo();
            return;
        }

        // Display banner
        ShowBanner();

        // Load configuration if exists
        LoadConfiguration();

        // Load service definitions from services.yaml
        _serviceConfig = ServiceDefinitionLoader.LoadServices();
        var dynamicMenuBuilder = GetDynamicMenuBuilder();
        var dynamicServices = dynamicMenuBuilder.GetEnabledServices();

        // Main menu loop
        bool exit = false;
        while (!exit)
        {
            // Build dynamic main menu choices
            var menuChoices = new List<string>();
            
            // Add hardcoded items first (Wallet and Vanity)
            menuChoices.Add(MenuWallet);
            menuChoices.Add(MenuVanity);
            
            // Add dynamic services from services.yaml (sorted by menu order)
            var serviceStatusMap = new Dictionary<string, string>();
            foreach (var service in dynamicServices)
            {
                var status = await dynamicMenuBuilder.GetServiceStatusIndicatorAsync(service);
                var label = $"{service.MenuLabel.PadRight(12)} {service.Name} {status}";
                menuChoices.Add(label);
                serviceStatusMap[label] = service.Id;
            }
            
            // Add Exit at the end
            menuChoices.Add(MenuExit);

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold green]Main Menu[/]")
                    .PageSize(15)
                    .AddChoices(menuChoices)
            );

            AnsiConsole.Clear();
            ShowBanner();

            // Handle menu choice
            if (choice == MenuWallet)
            {
                await ShowWalletMenuAsync();
            }
            else if (choice == MenuVanity)
            {
                await VanityCommands.GenerateVanityAddressAsync();
                AnsiConsole.MarkupLine("\n<= Press ENTER to return");
                Console.ReadLine();
            }
            else if (choice == MenuExit)
            {
                exit = true;
                AnsiConsole.MarkupLine("[bold yellow]Goodbye![/]");
            }
            else if (serviceStatusMap.TryGetValue(choice, out var serviceId))
            {
                // Handle dynamic service menu
                var service = dynamicServices.FirstOrDefault(s => s.Id == serviceId);
                if (service != null)
                {
                    await dynamicMenuBuilder.ShowServiceMenuAsync(service, ShowBanner);
                }
            }

            if (!exit)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[dim]Press ENTER to return to the main menu...[/]");
                Console.ReadLine();

                // Show background services status
                if (BackgroundServiceManager.HasRunningServices())
                {
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("[bold]Background Services:[/]");
                    BackgroundServiceManager.DisplayServices();
                }

                AnsiConsole.Clear();
                ShowBanner();
            }
        }

        // Stop all background services on exit
        BackgroundServiceManager.StopAllServices();
    }

    /// <summary>
    /// Show wallet management submenu
    /// </summary>
    static async Task ShowWalletMenuAsync()
    {
        var choices = new[]
        {
            "Create New Wallet",
            "Restore Wallet from Mnemonic",
            "Show Addresses",
            "Check Balance",
            "Send Funds",
            "Transaction History",
            "<= Back"
        };

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold green]Wallet Management[/]")
                .PageSize(10)
                .AddChoices(choices)
        );

        AnsiConsole.Clear();
        ShowBanner();

        switch (choice)
        {
            case "Create New Wallet":
                await WalletCommands.CreateNewWallet();
                break;
            case "Restore Wallet from Mnemonic":
                await WalletCommands.RestoreWallet();
                break;
            case "Show Addresses":
                WalletCommands.ShowAddresses();
                break;
            case "Check Balance":
                await TransactionCommands.CheckBalance();
                break;
            case "Send Funds":
                await TransactionCommands.SendFunds();
                break;
            case "Transaction History":
                await TransactionCommands.ShowTransactionHistory();
                break;
        }
    }

    static void ShowBanner()
    {
        var rule = new Rule("[red]PoCX HD Wallet[/]");
        var ruleLine = new Rule();
        ruleLine.RuleStyle("blue dim");
        rule.RuleStyle("blue dim");
        rule.LeftJustified();
        AnsiConsole.Write(ruleLine);
        AnsiConsole.Write(rule);
        AnsiConsole.Write(ruleLine);
        AnsiConsole.WriteLine();
    }

    static void LoadConfiguration()
    {
        try
        {
            var loadedSettings = SettingsManager.LoadSettings();
            // Copy all properties from loaded settings
            _settings.PlotDirectory = loadedSettings.PlotDirectory;
            _settings.WalletFilePath = loadedSettings.WalletFilePath;
            _settings.MinerConfigPath = loadedSettings.MinerConfigPath;
            _settings.BitcoinNodeHost = loadedSettings.BitcoinNodeHost;
            _settings.BitcoinNodePort = loadedSettings.BitcoinNodePort;
            _settings.DockerNetwork = loadedSettings.DockerNetwork;
            _settings.BitcoinContainerName = loadedSettings.BitcoinContainerName;
            _settings.MinerContainerName = loadedSettings.MinerContainerName;
            _settings.PlotterContainerName = loadedSettings.PlotterContainerName;
            _settings.ElectrsContainerName = loadedSettings.ElectrsContainerName;
            _settings.BitcoinNode = loadedSettings.BitcoinNode;
            _settings.Electrs = loadedSettings.Electrs;
            _settings.Miner = loadedSettings.Miner;
            _settings.Plotter = loadedSettings.Plotter;
        }
        catch
        {
            // Use defaults if configuration fails to load
        }
    }

    static void SaveConfiguration()
    {
        SettingsManager.SaveSettings(_settings);
        AnsiConsole.MarkupLine($"[green]√[/] Configuration saved to: appsettings.json");
    }

    /// <summary>
    /// Generic menu helper:
    /// - automatically appends the global back option `General_Back`
    /// - handlers must be in the same order as `choices` (no explicit back handler required)
    /// </summary>
    static async Task ShowMenuAsync(string title, MenuOptions[] choices, Func<Task>[] handlers)
    {
        if (choices == null) throw new ArgumentNullException(nameof(choices));
        if (handlers == null) throw new ArgumentNullException(nameof(handlers));
        if (choices.Length != handlers.Length) throw new ArgumentException("choices and handlers must have same length");

        // Append global back option automatically
        var promptChoices = choices.Concat(new[] { MenuOptions.General_Back }).ToArray();
        var promptHandlers = handlers.Concat(new Func<Task>[] { () => Task.CompletedTask }).ToArray();

        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<MenuOptions>()
                .Title($"[bold green]{title}[/]")
                .PageSize(10)
                .AddChoices(promptChoices)
                .UseConverter(opt => opt.ToDisplayString())
        );

        AnsiConsole.Clear();
        ShowBanner();

        var idx = Array.IndexOf(promptChoices, selection);
        if (idx < 0 || idx >= promptHandlers.Length) return;

        var handler = promptHandlers[idx];
        if (handler != null)
            await handler();
    }

    /// <summary>
    /// Show service banner with last 5 log lines if service is running
    /// </summary>
    static async Task ShowServiceBannerAsync(AppSettings settings, string? serviceType = null)
    {
        var docker = new DockerServiceManager();
        
        // Determine which containers to check based on service type
        var containersToCheck = new List<(string name, string label)>();
        
        if (serviceType == null || serviceType == "node")
        {
            containersToCheck.Add((settings.BitcoinContainerName, "Bitcoin Node"));
        }
        else if (serviceType == "miner")
        {
            containersToCheck.Add((settings.MinerContainerName, "Miner"));
        }
        else if (serviceType == "plotter")
        {
            containersToCheck.Add((settings.PlotterContainerName, "Plotter"));
        }
        else if (serviceType == "electrs")
        {
            containersToCheck.Add((settings.ElectrsContainerName, "Electrs Server"));
        }

        foreach (var (containerName, label) in containersToCheck)
        {
            var status = await docker.GetContainerStatusAsync(containerName);
            if (status == "running")
            {
                AnsiConsole.MarkupLine($"[bold green]● {label} is running[/]");
                var logs = await docker.GetContainerLogsAsync(containerName, 5);
                
                if (!string.IsNullOrWhiteSpace(logs))
                {
                    // Display last 5 log lines without box
                    var logLines = logs.Split('\n', StringSplitOptions.RemoveEmptyEntries).TakeLast(5);
                    foreach (var line in logLines)
                    {
                        AnsiConsole.MarkupLine($"[dim]{Markup.Escape(line)}[/]");
                    }
                }
                AnsiConsole.WriteLine();
            }
        }
    }

    /// <summary>
    /// Show Node Settings submenu with editable settings
    /// </summary>
    static Task ShowNodeSettingsMenuAsync()
    {
        bool back = false;
        while (!back)
        {
            var settingOptions = new[]
            {
                "Repository",
                "Tag",
                "RPC Port",
                "P2P Port",
                "Data Directory",
                "Additional Parameters",
                "<= Back"
            };

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold green]Node Settings[/]")
                    .PageSize(10)
                    .AddChoices(settingOptions)
            );

            AnsiConsole.Clear();
            ShowBanner();

            switch (choice)
            {
                case "Repository":
                    _settings.BitcoinNode.Repository = AnsiConsole.Ask("Enter Docker repository URL:", _settings.BitcoinNode.Repository);
                    SaveConfiguration();
                    AnsiConsole.MarkupLine("[green]✓[/] Repository updated");
                    break;
                case "Tag":
                    _settings.BitcoinNode.Tag = AnsiConsole.Ask("Enter Docker image tag:", _settings.BitcoinNode.Tag);
                    SaveConfiguration();
                    AnsiConsole.MarkupLine("[green]✓[/] Tag updated");
                    break;
                case "RPC Port":
                    _settings.BitcoinNode.RpcPort = AnsiConsole.Ask("Enter RPC port:", _settings.BitcoinNode.RpcPort);
                    SaveConfiguration();
                    AnsiConsole.MarkupLine("[green]✓[/] RPC Port updated");
                    break;
                case "P2P Port":
                    _settings.BitcoinNode.P2PPort = AnsiConsole.Ask("Enter P2P port:", _settings.BitcoinNode.P2PPort);
                    SaveConfiguration();
                    AnsiConsole.MarkupLine("[green]✓[/] P2P Port updated");
                    break;
                case "Data Directory":
                    _settings.BitcoinNode.DataDirectory = AnsiConsole.Ask("Enter data directory path:", _settings.BitcoinNode.DataDirectory);
                    SaveConfiguration();
                    AnsiConsole.MarkupLine("[green]✓[/] Data Directory updated");
                    break;
                case "Additional Parameters":
                    _settings.BitcoinNode.AdditionalParams = AnsiConsole.Ask("Enter additional bitcoind parameters:", _settings.BitcoinNode.AdditionalParams);
                    SaveConfiguration();
                    AnsiConsole.MarkupLine("[green]✓[/] Additional Parameters updated");
                    break;
                case "<= Back":
                    back = true;
                    break;
            }

            if (!back)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[dim]Press ENTER to continue...[/]");
                Console.ReadLine();
                AnsiConsole.Clear();
                ShowBanner();
            }
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Show Miner Settings submenu with editable settings
    /// </summary>
    static Task ShowMinerSettingsMenuAsync()
    {
        bool back = false;
        while (!back)
        {
            var settingOptions = new[]
            {
                "Repository",
                "Tag",
                "CPU Threads",
                "Use Direct I/O",
                "Show Progress",
                "Additional Parameters",
                "<= Back"
            };

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold green]Miner Settings[/]")
                    .PageSize(10)
                    .AddChoices(settingOptions)
            );

            AnsiConsole.Clear();
            ShowBanner();

            switch (choice)
            {
                case "Repository":
                    _settings.Miner.Repository = AnsiConsole.Ask("Enter Docker repository URL:", _settings.Miner.Repository);
                    SaveConfiguration();
                    AnsiConsole.MarkupLine("[green]✓[/] Repository updated");
                    break;
                case "Tag":
                    _settings.Miner.Tag = AnsiConsole.Ask("Enter Docker image tag:", _settings.Miner.Tag);
                    SaveConfiguration();
                    AnsiConsole.MarkupLine("[green]✓[/] Tag updated");
                    break;
                case "CPU Threads":
                    _settings.Miner.CpuThreads = AnsiConsole.Ask("Enter number of CPU threads:", _settings.Miner.CpuThreads);
                    SaveConfiguration();
                    AnsiConsole.MarkupLine("[green]✓[/] CPU Threads updated");
                    break;
                case "Use Direct I/O":
                    _settings.Miner.UseDirectIO = AnsiConsole.Confirm($"Use Direct I/O? (currently: {_settings.Miner.UseDirectIO})", _settings.Miner.UseDirectIO);
                    SaveConfiguration();
                    AnsiConsole.MarkupLine("[green]✓[/] Use Direct I/O updated");
                    break;
                case "Show Progress":
                    _settings.Miner.ShowProgress = AnsiConsole.Confirm($"Show Progress? (currently: {_settings.Miner.ShowProgress})", _settings.Miner.ShowProgress);
                    SaveConfiguration();
                    AnsiConsole.MarkupLine("[green]✓[/] Show Progress updated");
                    break;
                case "Additional Parameters":
                    _settings.Miner.AdditionalParams = AnsiConsole.Ask("Enter additional miner parameters:", _settings.Miner.AdditionalParams);
                    SaveConfiguration();
                    AnsiConsole.MarkupLine("[green]✓[/] Additional Parameters updated");
                    break;
                case "<= Back":
                    back = true;
                    break;
            }

            if (!back)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[dim]Press ENTER to continue...[/]");
                Console.ReadLine();
                AnsiConsole.Clear();
                ShowBanner();
            }
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Show Plotter Settings submenu with editable settings
    /// </summary>
    static Task ShowPlotterSettingsMenuAsync()
    {
        bool back = false;
        while (!back)
        {
            var settingOptions = new[]
            {
                "Repository",
                "Tag",
                "Default Warps",
                "CPU Threads",
                "Use Direct I/O",
                "Additional Parameters",
                "<= Back"
            };

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold green]Plotter Settings[/]")
                    .PageSize(10)
                    .AddChoices(settingOptions)
            );

            AnsiConsole.Clear();
            ShowBanner();

            switch (choice)
            {
                case "Repository":
                    _settings.Plotter.Repository = AnsiConsole.Ask("Enter Docker repository URL:", _settings.Plotter.Repository);
                    SaveConfiguration();
                    AnsiConsole.MarkupLine("[green]✓[/] Repository updated");
                    break;
                case "Tag":
                    _settings.Plotter.Tag = AnsiConsole.Ask("Enter Docker image tag:", _settings.Plotter.Tag);
                    SaveConfiguration();
                    AnsiConsole.MarkupLine("[green]✓[/] Tag updated");
                    break;
                case "Default Warps":
                    _settings.Plotter.DefaultWarps = AnsiConsole.Ask("Enter default number of warps (1 warp ≈ 1GB):", _settings.Plotter.DefaultWarps);
                    SaveConfiguration();
                    AnsiConsole.MarkupLine("[green]✓[/] Default Warps updated");
                    break;
                case "CPU Threads":
                    _settings.Plotter.CpuThreads = AnsiConsole.Ask("Enter number of CPU threads:", _settings.Plotter.CpuThreads);
                    SaveConfiguration();
                    AnsiConsole.MarkupLine("[green]✓[/] CPU Threads updated");
                    break;
                case "Use Direct I/O":
                    _settings.Plotter.UseDirectIO = AnsiConsole.Confirm($"Use Direct I/O? (currently: {_settings.Plotter.UseDirectIO})", _settings.Plotter.UseDirectIO);
                    SaveConfiguration();
                    AnsiConsole.MarkupLine("[green]✓[/] Use Direct I/O updated");
                    break;
                case "Additional Parameters":
                    _settings.Plotter.AdditionalParams = AnsiConsole.Ask("Enter additional plotter parameters:", _settings.Plotter.AdditionalParams);
                    SaveConfiguration();
                    AnsiConsole.MarkupLine("[green]✓[/] Additional Parameters updated");
                    break;
                case "<= Back":
                    back = true;
                    break;
            }

            if (!back)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[dim]Press ENTER to continue...[/]");
                Console.ReadLine();
                AnsiConsole.Clear();
                ShowBanner();
            }
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Show Electrs Settings submenu with editable settings
    /// </summary>
    static Task ShowElectrsSettingsMenuAsync()
    {
        bool back = false;
        while (!back)
        {
            var settingOptions = new[]
            {
                "Repository",
                "Tag",
                "HTTP Port",
                "RPC Port",
                "Testnet Port",
                "Data Directory",
                "Additional Parameters",
                "<= Back"
            };

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold green]Electrs Settings[/]")
                    .PageSize(10)
                    .AddChoices(settingOptions)
            );

            AnsiConsole.Clear();
            ShowBanner();

            switch (choice)
            {
                case "Repository":
                    _settings.Electrs.Repository = AnsiConsole.Ask("Enter Docker repository URL:", _settings.Electrs.Repository);
                    SaveConfiguration();
                    AnsiConsole.MarkupLine("[green]✓[/] Repository updated");
                    break;
                case "Tag":
                    _settings.Electrs.Tag = AnsiConsole.Ask("Enter Docker image tag:", _settings.Electrs.Tag);
                    SaveConfiguration();
                    AnsiConsole.MarkupLine("[green]✓[/] Tag updated");
                    break;
                case "HTTP Port":
                    _settings.Electrs.HttpPort = AnsiConsole.Ask("Enter HTTP API port:", _settings.Electrs.HttpPort);
                    SaveConfiguration();
                    AnsiConsole.MarkupLine("[green]✓[/] HTTP Port updated");
                    break;
                case "RPC Port":
                    _settings.Electrs.RpcPort = AnsiConsole.Ask("Enter Electrum RPC port:", _settings.Electrs.RpcPort);
                    SaveConfiguration();
                    AnsiConsole.MarkupLine("[green]✓[/] RPC Port updated");
                    break;
                case "Testnet Port":
                    _settings.Electrs.TestnetPort = AnsiConsole.Ask("Enter Testnet RPC port:", _settings.Electrs.TestnetPort);
                    SaveConfiguration();
                    AnsiConsole.MarkupLine("[green]✓[/] Testnet Port updated");
                    break;
                case "Data Directory":
                    _settings.Electrs.DataDirectory = AnsiConsole.Ask("Enter data directory path:", _settings.Electrs.DataDirectory);
                    SaveConfiguration();
                    AnsiConsole.MarkupLine("[green]✓[/] Data Directory updated");
                    break;
                case "Additional Parameters":
                    _settings.Electrs.AdditionalParams = AnsiConsole.Ask("Enter additional electrs parameters:", _settings.Electrs.AdditionalParams);
                    SaveConfiguration();
                    AnsiConsole.MarkupLine("[green]✓[/] Additional Parameters updated");
                    break;
                case "<= Back":
                    back = true;
                    break;
            }

            if (!back)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[dim]Press ENTER to continue...[/]");
                Console.ReadLine();
                AnsiConsole.Clear();
                ShowBanner();
            }
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Show Plotting submenu with status-aware toggle
    /// </summary>
    static async Task ShowPlottingMenuAsync()
    {
        bool back = false;
        while (!back)
        {
            var isRunning = await IsServiceRunningAsync(_settings.PlotterContainerName);
            var statusIndicator = isRunning ? "[green]●[/]" : "[red]●[/]";
            var toggleText = isRunning ? "Stop Plotter" : "Start Plotter";
            
            // Show banner and logs before menu
            await ShowServiceBannerAsync(_settings, "plotter");
            
            var choices = new[]
            {
                toggleText,
                "Create Plot",
                "View Logs",
                "Plotter Settings",
                "<= Back"
            };

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"[bold green]Plotting - Service: {statusIndicator}[/]")
                    .PageSize(10)
                    .AddChoices(choices)
            );

            AnsiConsole.Clear();
            ShowBanner();

            if (choice.Contains("Stop") || choice.Contains("Start"))
            {
                if (isRunning)
                {
                    await PlottingCommands.StopPlotterAsync(_settings);
                }
                else
                {
                    await PlottingCommands.CreatePlotAsync(_settings);
                }
            }
            else if (choice == "Create Plot")
            {
                await PlottingCommands.CreatePlotAsync(_settings);
            }
            else if (choice == "View Logs")
            {
                await PlottingCommands.ViewLogsAsync(_settings);
            }
            else if (choice == "Plotter Settings")
            {
                await ShowPlotterSettingsMenuAsync();
                continue; // Skip the "press ENTER" prompt
            }
            else if (choice == "<= Back")
            {
                back = true;
            }

            if (!back && choice != "Plotter Settings")
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[dim]Press ENTER to continue...[/]");
                Console.ReadLine();
                AnsiConsole.Clear();
                ShowBanner();
            }
        }
    }

    /// <summary>
    /// Show Mining submenu with status-aware toggle
    /// </summary>
    static async Task ShowMiningMenuAsync()
    {
        bool back = false;
        while (!back)
        {
            var isRunning = await IsServiceRunningAsync(_settings.MinerContainerName);
            var statusIndicator = isRunning ? "[green]●[/]" : "[red]●[/]";
            var toggleText = isRunning ? "Stop Miner" : "Start Miner";
            
            // Show banner and logs before menu
            await ShowServiceBannerAsync(_settings, "miner");
            
            var choices = new[]
            {
                toggleText,
                "Create Miner Config",
                "View Logs",
                "Miner Settings",
                "<= Back"
            };

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"[bold green]Mining - Service: {statusIndicator}[/]")
                    .PageSize(10)
                    .AddChoices(choices)
            );

            AnsiConsole.Clear();
            ShowBanner();

            if (choice.Contains("Stop") || choice.Contains("Start"))
            {
                if (isRunning)
                {
                    await MiningCommands.StopMiningAsync(_settings);
                }
                else
                {
                    await MiningCommands.StartMiningAsync(_settings);
                }
            }
            else if (choice == "Create Miner Config")
            {
                MiningCommands.CreateMinerConfig(_settings.MinerConfigPath);
            }
            else if (choice == "View Logs")
            {
                await MiningCommands.ViewLogsAsync(_settings);
            }
            else if (choice == "Miner Settings")
            {
                await ShowMinerSettingsMenuAsync();
                continue; // Skip the "press ENTER" prompt
            }
            else if (choice == "<= Back")
            {
                back = true;
            }

            if (!back && choice != "Miner Settings")
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[dim]Press ENTER to continue...[/]");
                Console.ReadLine();
                AnsiConsole.Clear();
                ShowBanner();
            }
        }
    }

    /// <summary>
    /// Show Node submenu with status-aware toggle and Electrs status
    /// </summary>
    static async Task ShowNodeMenuAsync()
    {
        bool back = false;
        while (!back)
        {
            var isRunning = await IsServiceRunningAsync(_settings.BitcoinContainerName);
            var statusIndicator = isRunning ? "[green]●[/]" : "[red]●[/]";
            var toggleText = isRunning ? "Stop Node" : "Start Node";
            
            // Show banner and logs before menu
            await ShowServiceBannerAsync(_settings, "node");
            
            var choices = new[]
            {
                toggleText,
                "Import Wallet from File",
                "View Logs",
                "Node Settings",
                "<= Back"
            };

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"[bold green]Bitcoin-PoCX Node - Service: {statusIndicator}[/]")
                    .PageSize(10)
                    .AddChoices(choices)
            );

            AnsiConsole.Clear();
            ShowBanner();

            if (choice.Contains("Stop") || choice.Contains("Start"))
            {
                if (isRunning)
                {
                    await NodeCommands.StopNodeAsync(_settings);
                }
                else
                {
                    var dataDir = AnsiConsole.Ask<string>("Data directory (or press Enter for default):", "");
                    await NodeCommands.StartNodeAsync(_settings, string.IsNullOrWhiteSpace(dataDir) ? null : dataDir);
                }
            }
            else if (choice == "Import Wallet from File")
            {
                await WalletCommands.ImportWalletFromFileAsync(_settings);
            }
            else if (choice == "View Logs")
            {
                await NodeCommands.ViewLogsAsync(_settings);
            }
            else if (choice == "Node Settings")
            {
                await ShowNodeSettingsMenuAsync();
                continue; // Skip the "press ENTER" prompt
            }
            else if (choice == "<= Back")
            {
                back = true;
            }

            if (!back && choice != "Node Settings")
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[dim]Press ENTER to continue...[/]");
                Console.ReadLine();
                AnsiConsole.Clear();
                ShowBanner();
            }
        }
    }

    /// <summary>
    /// Show Electrs submenu
    /// </summary>
    static async Task ShowElectrsMenuAsync()
    {
        bool back = false;
        while (!back)
        {
            var isRunning = await IsServiceRunningAsync(_settings.ElectrsContainerName);
            var statusIndicator = isRunning ? "[green]●[/]" : "[red]●[/]";
            var toggleText = isRunning ? "Stop Electrs" : "Start Electrs";
            
            // Show banner and logs before menu
            await ShowServiceBannerAsync(_settings, "electrs");
            
            var choices = new[]
            {
                toggleText,
                "View Logs",
                "Electrs Settings",
                "<= Back"
            };

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"[bold green]Electrs Server - Service: {statusIndicator}[/]")
                    .PageSize(10)
                    .AddChoices(choices)
            );

            AnsiConsole.Clear();
            ShowBanner();

            if (choice.Contains("Stop") || choice.Contains("Start"))
            {
                if (isRunning)
                {
                    await ElectrsCommands.StopElectrsAsync(_settings);
                }
                else
                {
                    await ElectrsCommands.StartElectrsAsync(_settings);
                }
            }
            else if (choice == "View Logs")
            {
                await ElectrsCommands.ViewLogsAsync(_settings);
            }
            else if (choice == "Electrs Settings")
            {
                await ShowElectrsSettingsMenuAsync();
                continue; // Skip the "press ENTER" prompt
            }
            else if (choice == "<= Back")
            {
                back = true;
            }

            if (!back && choice != "Electrs Settings")
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[dim]Press ENTER to continue...[/]");
                Console.ReadLine();
                AnsiConsole.Clear();
                ShowBanner();
            }
        }
    }
}

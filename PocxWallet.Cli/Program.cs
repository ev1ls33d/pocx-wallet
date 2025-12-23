using System;
using System.Linq;
using System.Threading.Tasks;
using PocxWallet.Cli.Commands;
using PocxWallet.Cli.Configuration;
using PocxWallet.Cli.Services;
using Spectre.Console;

namespace PocxWallet.Cli;

// Single enum mit Präfix-Notation für Haupt- und Untermenüs
enum MenuOptions
{
    // Main
    Main_WalletManagement,
    Main_Plotting,
    Main_Mining,
    Main_VanityAddressGenerator,
    Main_BitcoinPoCXNode,
    Main_Electrs,
    Main_Exit,

    // Wallet submenu
    Wallet_CreateNewWallet,
    Wallet_RestoreWalletFromMnemonic,
    Wallet_ShowAddresses,
    Wallet_CheckBalance,
    Wallet_SendFunds,
    Wallet_TransactionHistory,

    // Plotting submenu
    Plotting_ToggleService,
    Plotting_CreatePlot,
    Plotting_ViewLogs,
    Plotting_Settings,

    // Mining submenu
    Mining_ToggleService,
    Mining_CreateMinerConfig,
    Mining_ViewLogs,
    Mining_Settings,

    // Node submenu
    Node_ToggleService,
    Node_ImportWalletFromFile,
    Node_ViewLogs,
    Node_Settings,

    // Electrs submenu
    Electrs_ToggleService,
    Electrs_ViewLogs,
    Electrs_Settings,

    // Docker submenu
    Docker_CheckStatus,
    Docker_Setup,

    // General back option (einmalig, für alle Submenus)
    General_Back
}

static class MenuOptionsExtensions
{
    public static string ToDisplayString(this MenuOptions option) =>
        option switch
        {
            // Main - These will be dynamically updated with status in ShowMainMenu
            MenuOptions.Main_WalletManagement =>                Markup.Escape("[Wallet]    Wallet Management"),
            MenuOptions.Main_Plotting =>                        Markup.Escape("[[Plot]]      Plotting"),
            MenuOptions.Main_Mining =>                          Markup.Escape("[[Mine]]      Mining"),
            MenuOptions.Main_VanityAddressGenerator =>          Markup.Escape("[Vanity]    Vanity Address Generator"),
            MenuOptions.Main_BitcoinPoCXNode =>                 Markup.Escape("[[Node]]      Bitcoin-PoCX Node"),
            MenuOptions.Main_Electrs =>                         Markup.Escape("[[Electrs]]   Electrs Server"),
            MenuOptions.Main_Exit =>                            Markup.Escape("[Exit]      Exit"),

            // Wallet
            MenuOptions.Wallet_CreateNewWallet =>               Markup.Escape("Create New Wallet"),
            MenuOptions.Wallet_RestoreWalletFromMnemonic =>     Markup.Escape("Restore Wallet from Mnemonic"),
            MenuOptions.Wallet_ShowAddresses =>                 Markup.Escape("Show Addresses"),
            MenuOptions.Wallet_CheckBalance =>                  Markup.Escape("Check Balance"),
            MenuOptions.Wallet_SendFunds =>                     Markup.Escape("Send Funds"),
            MenuOptions.Wallet_TransactionHistory =>            Markup.Escape("Transaction History"),

            // Plotting - Toggle will be updated dynamically
            MenuOptions.Plotting_ToggleService =>               Markup.Escape("Toggle Plotter Service"),
            MenuOptions.Plotting_CreatePlot =>                  Markup.Escape("Create Plot"),
            MenuOptions.Plotting_ViewLogs =>                    Markup.Escape("View Logs"),
            MenuOptions.Plotting_Settings =>                    Markup.Escape("Plotter Settings"),

            // Mining - Toggle will be updated dynamically
            MenuOptions.Mining_ToggleService =>                 Markup.Escape("Toggle Miner Service"),
            MenuOptions.Mining_CreateMinerConfig =>             Markup.Escape("Create Miner Config"),
            MenuOptions.Mining_ViewLogs =>                      Markup.Escape("View Logs"),
            MenuOptions.Mining_Settings =>                      Markup.Escape("Miner Settings"),

            // Node - Toggle will be updated dynamically
            MenuOptions.Node_ToggleService =>                   Markup.Escape("Toggle Node Service"),
            MenuOptions.Node_ImportWalletFromFile =>            Markup.Escape("Import Wallet from File"),
            MenuOptions.Node_ViewLogs =>                        Markup.Escape("View Logs"),
            MenuOptions.Node_Settings =>                        Markup.Escape("Node Settings"),

            // Electrs - Toggle will be updated dynamically
            MenuOptions.Electrs_ToggleService =>                Markup.Escape("Toggle Electrs Service"),
            MenuOptions.Electrs_ViewLogs =>                     Markup.Escape("View Logs"),
            MenuOptions.Electrs_Settings =>                     Markup.Escape("Electrs Settings"),

            // Docker
            MenuOptions.Docker_CheckStatus =>                   Markup.Escape("Check Docker Status"),
            MenuOptions.Docker_Setup =>                         Markup.Escape("Setup Docker"),

            // General
            MenuOptions.General_Back =>                         Markup.Escape("<= Back"),

            // Fallback
            _ => Markup.Escape(option.ToString())
        };
}

class Program
{
    private static readonly AppSettings _settings = new();
    private static DockerServiceManager? _dockerManager;

    private static DockerServiceManager GetDockerManager()
    {
        if (_dockerManager == null)
        {
            _dockerManager = new DockerServiceManager();
        }
        return _dockerManager;
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

        // Main menu loop
        bool exit = false;
        while (!exit)
        {
            var mainChoices = Enum.GetValues<MenuOptions>()
                .Cast<MenuOptions>()
                .Where(v => v.ToString().StartsWith("Main_"))
                .ToArray();

            // Get service statuses for display
            var plotterStatus = await GetServiceStatusIndicatorAsync(_settings.PlotterContainerName);
            var minerStatus = await GetServiceStatusIndicatorAsync(_settings.MinerContainerName);
            var nodeStatus = await GetServiceStatusIndicatorAsync(_settings.BitcoinContainerName);
            var electrsStatus = await GetServiceStatusIndicatorAsync(_settings.ElectrsContainerName);

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<MenuOptions>()
                    .Title("[bold green]Main Menu[/]")
                    .PageSize(10)
                    .AddChoices(mainChoices)
                    .UseConverter(opt => opt switch
                    {
                        MenuOptions.Main_Plotting => $"[[Plot]]      Plotting {plotterStatus}",
                        MenuOptions.Main_Mining => $"[[Mine]]      Mining {minerStatus}",
                        MenuOptions.Main_BitcoinPoCXNode => $"[[Node]]      Bitcoin-PoCX Node {nodeStatus}",
                        MenuOptions.Main_Electrs => $"[[Electrs]]   Electrs Server {electrsStatus}",
                        _ => opt.ToDisplayString()
                    })
            );

            AnsiConsole.Clear();
            ShowBanner();

            switch (choice)
            {
                case MenuOptions.Main_WalletManagement:
                    await ShowMenuAsync(
                        "Wallet Management",
                        Enum.GetValues<MenuOptions>().Cast<MenuOptions>().Where(v => v.ToString().StartsWith("Wallet_")).ToArray(),
                        new Func<Task> []
                        {
                            async () => await WalletCommands.CreateNewWallet(), // CreateNewWallet
                            async () => await WalletCommands.RestoreWallet(), // RestoreWallet
                            () => { WalletCommands.ShowAddresses(); return Task.CompletedTask; }, // ShowAddresses
                            async () => await TransactionCommands.CheckBalance(), // CheckBalance
                            async () => await TransactionCommands.SendFunds(), // SendFunds
                            async () => await TransactionCommands.ShowTransactionHistory() // TransactionHistory
                        });
                    break;

                case MenuOptions.Main_Plotting:
                    await ShowPlottingMenuAsync();
                    break;

                case MenuOptions.Main_Mining:
                    await ShowMiningMenuAsync();
                    break;

                case MenuOptions.Main_VanityAddressGenerator:
                    await VanityCommands.GenerateVanityAddressAsync();
                    AnsiConsole.MarkupLine("\n<= Press ENTER to return");
                    Console.ReadLine();
                    break;

                case MenuOptions.Main_BitcoinPoCXNode:
                    await ShowNodeMenuAsync();
                    break;

                case MenuOptions.Main_Electrs:
                    await ShowElectrsMenuAsync();
                    break;

                case MenuOptions.Main_Exit:
                    exit = true;
                    AnsiConsole.MarkupLine("[bold yellow]Goodbye![/]");
                    break;
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

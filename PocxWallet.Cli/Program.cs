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
    Main_Exit,

    // Wallet submenu
    Wallet_CreateNewWallet,
    Wallet_RestoreWalletFromMnemonic,
    Wallet_ShowAddresses,
    Wallet_CheckBalance,
    Wallet_SendFunds,
    Wallet_TransactionHistory,

    // Plotting submenu
    Plotting_CreatePlot,
    Plotting_ViewLogs,
    Plotting_Settings,

    // Mining submenu
    Mining_StartMining,
    Mining_StopMining,
    Mining_ShowMiningStatus,
    Mining_ViewLogs,
    Mining_CreateMinerConfig,
    Mining_Settings,

    // Node submenu
    Node_StartNode,
    Node_StopNode,
    Node_ShowNodeStatus,
    Node_ViewLogs,
    Node_EnableElectrs,
    Node_Settings,

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
            // Main
            MenuOptions.Main_WalletManagement =>                Markup.Escape("[Wallet]    Wallet Management"),
            MenuOptions.Main_Plotting =>                        Markup.Escape("[Plot]      Plotting"),
            MenuOptions.Main_Mining =>                          Markup.Escape("[Mine]      Mining"),
            MenuOptions.Main_VanityAddressGenerator =>          Markup.Escape("[Vanity]    Vanity Address Generator"),
            MenuOptions.Main_BitcoinPoCXNode =>                 Markup.Escape("[Node]      Bitcoin-PoCX Node"),
            MenuOptions.Main_Exit =>                            Markup.Escape("[Exit]      Exit"),

            // Wallet
            MenuOptions.Wallet_CreateNewWallet =>               Markup.Escape("Create New Wallet"),
            MenuOptions.Wallet_RestoreWalletFromMnemonic =>     Markup.Escape("Restore Wallet from Mnemonic"),
            MenuOptions.Wallet_ShowAddresses =>                 Markup.Escape("Show Addresses"),
            MenuOptions.Wallet_CheckBalance =>                  Markup.Escape("Check Balance"),
            MenuOptions.Wallet_SendFunds =>                     Markup.Escape("Send Funds"),
            MenuOptions.Wallet_TransactionHistory =>            Markup.Escape("Transaction History"),

            // Plotting
            MenuOptions.Plotting_CreatePlot =>                  Markup.Escape("Create Plot"),
            MenuOptions.Plotting_ViewLogs =>                    Markup.Escape("View Logs"),
            MenuOptions.Plotting_Settings =>                    Markup.Escape("Plotter Settings"),

            // Mining
            MenuOptions.Mining_StartMining =>                   Markup.Escape("Start Mining"),
            MenuOptions.Mining_StopMining =>                    Markup.Escape("Stop Mining"),
            MenuOptions.Mining_ShowMiningStatus =>              Markup.Escape("Show Mining Status"),
            MenuOptions.Mining_ViewLogs =>                      Markup.Escape("View Logs"),
            MenuOptions.Mining_CreateMinerConfig =>             Markup.Escape("Create Miner Config"),
            MenuOptions.Mining_Settings =>                      Markup.Escape("Miner Settings"),

            // Node
            MenuOptions.Node_StartNode =>                       Markup.Escape("Start Node"),
            MenuOptions.Node_StopNode =>                        Markup.Escape("Stop Node"),
            MenuOptions.Node_ShowNodeStatus =>                  Markup.Escape("Show Node Status"),
            MenuOptions.Node_ViewLogs =>                        Markup.Escape("View Logs"),
            MenuOptions.Node_EnableElectrs =>                   Markup.Escape("Toggle Electrs (Electrum Server)"),
            MenuOptions.Node_Settings =>                        Markup.Escape("Node Settings"),

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

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<MenuOptions>()
                    .Title("[bold green]Main Menu[/]")
                    .PageSize(10)
                    .AddChoices(mainChoices)
                    .UseConverter(opt => opt.ToDisplayString())
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
                    // Show last 5 log lines if service is running
                    await ShowServiceBannerAsync(_settings, "plotter");
                    
                    await ShowMenuAsync(
                        "Plotting",
                        Enum.GetValues<MenuOptions>().Cast<MenuOptions>().Where(v => v.ToString().StartsWith("Plotting_")).ToArray(),
                        new Func<Task>[]
                        {
                            async () => await PlottingCommands.CreatePlotAsync(_settings),
                            async () => await PlottingCommands.ViewLogsAsync(_settings),
                            () =>
                            {
                                AnsiConsole.MarkupLine("[bold yellow]Plotter Settings (Service-specific)[/]");
                                AnsiConsole.MarkupLine("[dim]Configure plotter-specific environment variables and parameters here[/]");
                                AnsiConsole.MarkupLine("[dim]Press ENTER to return[/]");
                                Console.ReadLine();
                                return Task.CompletedTask;
                            }
                        });
                    break;

                case MenuOptions.Main_Mining:
                    // Show last 5 log lines if service is running
                    await ShowServiceBannerAsync(_settings, "miner");
                    
                    await ShowMenuAsync(
                        "Mining",
                        Enum.GetValues<MenuOptions>().Cast<MenuOptions>().Where(v => v.ToString().StartsWith("Mining_")).ToArray(),
                        new Func<Task>[]
                        {
                            async () => await MiningCommands.StartMiningAsync(_settings),
                            async () => await MiningCommands.StopMiningAsync(_settings),
                            async () => await MiningCommands.ShowMiningStatusAsync(_settings),
                            async () => await MiningCommands.ViewLogsAsync(_settings),
                            () => { MiningCommands.CreateMinerConfig(_settings.MinerConfigPath); return Task.CompletedTask; },
                            () =>
                            {
                                AnsiConsole.MarkupLine("[bold yellow]Miner Settings (Service-specific)[/]");
                                AnsiConsole.MarkupLine("[dim]Configure miner-specific environment variables and parameters here[/]");
                                AnsiConsole.MarkupLine("[dim]Press ENTER to return[/]");
                                Console.ReadLine();
                                return Task.CompletedTask;
                            }
                        });
                    break;

                case MenuOptions.Main_VanityAddressGenerator:
                    await VanityCommands.GenerateVanityAddressAsync();
                    AnsiConsole.MarkupLine("\n<= Press ENTER to return");
                    Console.ReadLine();
                    break;

                case MenuOptions.Main_BitcoinPoCXNode:
                    // Show last 5 log lines if service is running
                    await ShowServiceBannerAsync(_settings);
                    
                    await ShowMenuAsync(
                        "Bitcoin-PoCX Node",
                        Enum.GetValues<MenuOptions>().Cast<MenuOptions>().Where(v => v.ToString().StartsWith("Node_")).ToArray(),
                        new Func<Task>[]
                        {
                            async () =>
                            {
                                var dataDir = AnsiConsole.Ask<string>("Data directory (or press Enter for default):", "");
                                await NodeCommands.StartNodeAsync(_settings, string.IsNullOrWhiteSpace(dataDir) ? null : dataDir);
                            },
                            async () => await NodeCommands.StopNodeAsync(_settings),
                            async () => await NodeCommands.ShowNodeStatusAsync(_settings),
                            async () => await NodeCommands.ViewLogsAsync(_settings),
                            () =>
                            {
                                _settings.EnableElectrs = !_settings.EnableElectrs;
                                AnsiConsole.MarkupLine($"[green]✓[/] Electrs: {(_settings.EnableElectrs ? "Enabled" : "Disabled")}");
                                AnsiConsole.MarkupLine("[dim]Electrs will {0} with the node on next start[/]", _settings.EnableElectrs ? "start" : "not start");
                                return Task.CompletedTask;
                            },
                            () =>
                            {
                                AnsiConsole.MarkupLine("[bold yellow]Node Settings (Service-specific)[/]");
                                AnsiConsole.MarkupLine($"[dim]Electrs Enabled: {_settings.EnableElectrs}[/]");
                                AnsiConsole.MarkupLine("[dim]Configure node-specific environment variables and parameters here[/]");
                                AnsiConsole.MarkupLine("[dim]Press ENTER to return[/]");
                                Console.ReadLine();
                                return Task.CompletedTask;
                            }
                        });
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
            _settings.EnableElectrs = loadedSettings.EnableElectrs;
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
            if (settings.EnableElectrs)
                containersToCheck.Add((settings.ElectrsContainerName, "Electrs Server"));
        }
        else if (serviceType == "miner")
        {
            containersToCheck.Add((settings.MinerContainerName, "Miner"));
        }
        else if (serviceType == "plotter")
        {
            containersToCheck.Add((settings.PlotterContainerName, "Plotter"));
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
                    // Limit each log line display
                    var logLines = logs.Split('\n', StringSplitOptions.RemoveEmptyEntries).TakeLast(5);
                    var panel = new Panel(string.Join("\n", logLines))
                    {
                        Header = new PanelHeader($"[dim]Last 5 log lines[/]"),
                        Border = BoxBorder.Rounded,
                        BorderStyle = new Style(Color.Grey)
                    };
                    AnsiConsole.Write(panel);
                }
                AnsiConsole.WriteLine();
            }
        }
    }
}

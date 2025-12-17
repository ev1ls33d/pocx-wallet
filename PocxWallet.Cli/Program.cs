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
    Main_Settings,
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

    // Mining submenu
    Mining_StartMining,
    Mining_StopMining,
    Mining_ShowMiningStatus,
    Mining_CreateMinerConfig,

    // Node submenu
    Node_StartNode,
    Node_StopNode,
    Node_ShowNodeStatus,

    // Settings submenu
    Settings_ViewCurrentSettings,
    Settings_ChangePoCXBinariesPath,
    Settings_ChangePlotDirectory,
    Settings_ChangeWalletFilePath,
    Settings_ChangeMinerConfigPath,
    Settings_SaveSettings,

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
            MenuOptions.Main_Settings =>                        Markup.Escape("[Settings]  Settings"),
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

            // Mining
            MenuOptions.Mining_StartMining =>                   Markup.Escape("Start Mining"),
            MenuOptions.Mining_StopMining =>                    Markup.Escape("Stop Mining"),
            MenuOptions.Mining_ShowMiningStatus =>              Markup.Escape("Show Mining Status"),
            MenuOptions.Mining_CreateMinerConfig =>             Markup.Escape("Create Miner Config"),

            // Node
            MenuOptions.Node_StartNode =>                       Markup.Escape("Start Node"),
            MenuOptions.Node_StopNode =>                        Markup.Escape("Stop Node"),
            MenuOptions.Node_ShowNodeStatus =>                  Markup.Escape("Show Node Status"),

            // Settings
            MenuOptions.Settings_ViewCurrentSettings =>         Markup.Escape("View Current Settings"),
            MenuOptions.Settings_ChangePoCXBinariesPath =>      Markup.Escape("Change PoCX Binaries Path"),
            MenuOptions.Settings_ChangePlotDirectory =>         Markup.Escape("Change Plot Directory"),
            MenuOptions.Settings_ChangeWalletFilePath =>        Markup.Escape("Change Wallet File Path"),
            MenuOptions.Settings_ChangeMinerConfigPath =>       Markup.Escape("Change Miner Config Path"),
            MenuOptions.Settings_SaveSettings =>                Markup.Escape("Save Settings"),

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
                            () => { WalletCommands.CreateNewWallet(); return Task.CompletedTask; }, // CreateNewWallet
                            () => { WalletCommands.RestoreWallet(); return Task.CompletedTask; }, // RestoreWallet
                            () => { WalletCommands.ShowAddresses(); return Task.CompletedTask; }, // ShowAddresses
                            async () => await TransactionCommands.CheckBalance(), // CheckBalance
                            async () => await TransactionCommands.SendFunds(), // SendFunds
                            async () => await TransactionCommands.ShowTransactionHistory() // TransactionHistory
                        });
                    break;

                case MenuOptions.Main_Plotting:
                    await ShowMenuAsync(
                        "Plotting",
                        Enum.GetValues<MenuOptions>().Cast<MenuOptions>().Where(v => v.ToString().StartsWith("Plotting_")).ToArray(),
                        new Func<Task>[]
                        {
                            async () => await PlottingCommands.CreatePlotAsync(_settings.PoCXBinariesPath)
                        });
                    break;

                case MenuOptions.Main_Mining:
                    await ShowMenuAsync(
                        "Mining",
                        Enum.GetValues<MenuOptions>().Cast<MenuOptions>().Where(v => v.ToString().StartsWith("Mining_")).ToArray(),
                        new Func<Task>[]
                        {
                            () => { MiningCommands.StartMining(_settings.PoCXBinariesPath, _settings.MinerConfigPath); return Task.CompletedTask; },
                            () => { MiningCommands.StopMining(); return Task.CompletedTask; },
                            () => { MiningCommands.ShowMiningStatus(); return Task.CompletedTask; },
                            () => { MiningCommands.CreateMinerConfig(_settings.MinerConfigPath); return Task.CompletedTask; }
                        });
                    break;

                case MenuOptions.Main_VanityAddressGenerator:
                    await VanityCommands.GenerateVanityAddressAsync();
                    AnsiConsole.MarkupLine("\n<= Press ENTER to return");
                    Console.ReadLine();
                    break;

                case MenuOptions.Main_BitcoinPoCXNode:
                    await ShowMenuAsync(
                        "Bitcoin-PoCX Node",
                        Enum.GetValues<MenuOptions>().Cast<MenuOptions>().Where(v => v.ToString().StartsWith("Node_")).ToArray(),
                        new Func<Task>[]
                        {
                            async () =>
                            {
                                var dataDir = AnsiConsole.Ask<string>("Data directory (or press Enter for default):", "");
                                NodeCommands.StartNode(
                                    _settings.BitcoinBinariesPath,
                                    string.IsNullOrWhiteSpace(dataDir) ? null : dataDir,
                                    _settings.BitcoinNodePort);
                                await Task.CompletedTask;
                            },
                            () => { NodeCommands.StopNode(); return Task.CompletedTask; },
                            () => { NodeCommands.ShowNodeStatus(); return Task.CompletedTask; }
                        });
                    break;

                case MenuOptions.Main_Settings:
                    await ShowMenuAsync(
                        "Settings",
                        Enum.GetValues<MenuOptions>().Cast<MenuOptions>().Where(v => v.ToString().StartsWith("Settings_")).ToArray(),
                        new Func<Task>[]
                        {
                            () =>
                            {
                                var table = new Table();
                                table.AddColumn("Setting");
                                table.AddColumn("Value");
                                table.AddRow("PoCX Binaries Path", _settings.PoCXBinariesPath);
                                table.AddRow("Plot Directory", _settings.PlotDirectory);
                                table.AddRow("Wallet File Path", _settings.WalletFilePath);
                                table.AddRow("Miner Config Path", _settings.MinerConfigPath);
                                AnsiConsole.Write(table);
                                return Task.CompletedTask;
                            },
                            () =>
                            {
                                _settings.PoCXBinariesPath = AnsiConsole.Ask<string>(
                                    "Enter PoCX binaries path:",
                                    _settings.PoCXBinariesPath);
                                return Task.CompletedTask;
                            },
                            () =>
                            {
                                _settings.PlotDirectory = AnsiConsole.Ask<string>(
                                    "Enter plot directory:",
                                    _settings.PlotDirectory);
                                return Task.CompletedTask;
                            },
                            () =>
                            {
                                _settings.WalletFilePath = AnsiConsole.Ask<string>(
                                    "Enter wallet file path:",
                                    _settings.WalletFilePath);
                                return Task.CompletedTask;
                            },
                            () =>
                            {
                                _settings.MinerConfigPath = AnsiConsole.Ask<string>(
                                    "Enter miner config path:",
                                    _settings.MinerConfigPath);
                                return Task.CompletedTask;
                            },
                            () =>
                            {
                                SaveConfiguration();
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
        var configPath = "appsettings.json";
        if (File.Exists(configPath))
        {
            try
            {
                var json = File.ReadAllText(configPath);
                var config = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json);
                if (config != null)
                {
                    _settings.PoCXBinariesPath = config.PoCXBinariesPath;
                    _settings.PlotDirectory = config.PlotDirectory;
                    _settings.WalletFilePath = config.WalletFilePath;
                    _settings.MinerConfigPath = config.MinerConfigPath;
                }
            }
            catch
            {
                // Use defaults if configuration fails to load
            }
        }
    }

    static void SaveConfiguration()
    {
        var configPath = "appsettings.json";
        var json = System.Text.Json.JsonSerializer.Serialize(_settings, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(configPath, json);
        AnsiConsole.MarkupLine($"[green][OK][/] Configuration saved to: {configPath}");
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
}

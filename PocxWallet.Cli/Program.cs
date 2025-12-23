using PocxWallet.Cli.Commands;
using PocxWallet.Cli.Configuration;
using PocxWallet.Cli.Services;
using Spectre.Console;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PocxWallet.Cli;

class Program
{
    private static readonly AppSettings _settings = new();
    private static DockerServiceManager? _dockerManager;
    private static ServiceConfiguration? _serviceConfig;
    private static DynamicServiceMenuBuilder? _dynamicMenuBuilder;

    // Constants for hardcoded menu items
    private static string MenuWallet = $"{Markup.Escape("[Wallet]").PadRight(15)} Wallet Management";
    private static string MenuVanity = $"{Markup.Escape("[Vanity]").PadRight(15)} Vanity Address Generator";
    private static string MenuExit   = $"{Markup.Escape("[Exit]").PadRight(15)} Exit";

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
                var label = $"{Markup.Escape(service.MenuLabel).PadRight(15)} {service.Name} {status}";
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

}

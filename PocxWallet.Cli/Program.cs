using PocxWallet.Cli.Commands;
using PocxWallet.Cli.Configuration;
using Spectre.Console;

namespace PocxWallet.Cli;

class Program
{
    private static readonly AppSettings _settings = new();

    static async Task Main(string[] args)
    {
        // Check if demo mode is requested
        if (args.Length > 0 && args[0] == "--demo")
        {
            Demo.RunWalletDemo();
            return;
        }

        // Check if running in interactive terminal
        if (!Console.IsInputRedirected && Console.KeyAvailable == false)
        {
            Console.WriteLine("PoCX Wallet requires an interactive terminal.");
            Console.WriteLine("Run with --demo flag to see a feature demonstration:");
            Console.WriteLine("  dotnet run -- --demo");
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
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold green]PoCX Wallet - Main Menu[/]")
                    .PageSize(10)
                    .AddChoices(
                        "💰 Wallet Management",
                        "📊 Plotting",
                        "⛏️  Mining",
                        "✨ Vanity Address Generator",
                        "⚙️  Settings",
                        "🚪 Exit"
                    ));

            AnsiConsole.Clear();
            ShowBanner();

            switch (choice)
            {
                case "💰 Wallet Management":
                    await HandleWalletMenuAsync();
                    break;
                case "📊 Plotting":
                    await HandlePlottingMenuAsync();
                    break;
                case "⛏️  Mining":
                    HandleMiningMenu();
                    break;
                case "✨ Vanity Address Generator":
                    await VanityCommands.GenerateVanityAddressAsync();
                    break;
                case "⚙️  Settings":
                    HandleSettingsMenu();
                    break;
                case "🚪 Exit":
                    exit = true;
                    AnsiConsole.MarkupLine("[bold yellow]Goodbye![/]");
                    break;
            }

            if (!exit)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
                Console.ReadKey(true);
                AnsiConsole.Clear();
                ShowBanner();
            }
        }
    }

    static void ShowBanner()
    {
        var banner = new FigletText("PoCX Wallet")
            .Centered()
            .Color(Color.Green);

        AnsiConsole.Write(banner);
        AnsiConsole.MarkupLine("[dim]A .NET 9 HD Wallet for Proof of Capacity X[/]");
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
        AnsiConsole.MarkupLine($"[green]✓[/] Configuration saved to: {configPath}");
    }

    static async Task HandleWalletMenuAsync()
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold green]Wallet Management[/]")
                .AddChoices(
                    "Create New Wallet",
                    "Restore Wallet from Mnemonic",
                    "Show Addresses",
                    "Back to Main Menu"
                ));

        switch (choice)
        {
            case "Create New Wallet":
                WalletCommands.CreateNewWallet();
                break;
            case "Restore Wallet from Mnemonic":
                WalletCommands.RestoreWallet();
                break;
            case "Show Addresses":
                WalletCommands.ShowAddresses();
                break;
        }

        await Task.CompletedTask;
    }

    static async Task HandlePlottingMenuAsync()
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold green]Plotting[/]")
                .AddChoices(
                    "Create Plot",
                    "Verify Plot",
                    "Back to Main Menu"
                ));

        switch (choice)
        {
            case "Create Plot":
                await PlottingCommands.CreatePlotAsync(_settings.PoCXBinariesPath);
                break;
            case "Verify Plot":
                await PlottingCommands.VerifyPlotAsync(_settings.PoCXBinariesPath);
                break;
        }
    }

    static void HandleMiningMenu()
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold green]Mining[/]")
                .AddChoices(
                    "Start Mining",
                    "Stop Mining",
                    "Show Mining Status",
                    "Create Miner Config",
                    "Back to Main Menu"
                ));

        switch (choice)
        {
            case "Start Mining":
                MiningCommands.StartMining(_settings.PoCXBinariesPath, _settings.MinerConfigPath);
                break;
            case "Stop Mining":
                MiningCommands.StopMining();
                break;
            case "Show Mining Status":
                MiningCommands.ShowMiningStatus();
                break;
            case "Create Miner Config":
                MiningCommands.CreateMinerConfig(_settings.MinerConfigPath);
                break;
        }
    }

    static void HandleSettingsMenu()
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold green]Settings[/]")
                .AddChoices(
                    "View Current Settings",
                    "Change PoCX Binaries Path",
                    "Change Plot Directory",
                    "Change Wallet File Path",
                    "Change Miner Config Path",
                    "Save Settings",
                    "Back to Main Menu"
                ));

        switch (choice)
        {
            case "View Current Settings":
                var table = new Table();
                table.AddColumn("Setting");
                table.AddColumn("Value");
                table.AddRow("PoCX Binaries Path", _settings.PoCXBinariesPath);
                table.AddRow("Plot Directory", _settings.PlotDirectory);
                table.AddRow("Wallet File Path", _settings.WalletFilePath);
                table.AddRow("Miner Config Path", _settings.MinerConfigPath);
                AnsiConsole.Write(table);
                break;

            case "Change PoCX Binaries Path":
                _settings.PoCXBinariesPath = AnsiConsole.Ask<string>(
                    "Enter PoCX binaries path:",
                    _settings.PoCXBinariesPath);
                break;

            case "Change Plot Directory":
                _settings.PlotDirectory = AnsiConsole.Ask<string>(
                    "Enter plot directory:",
                    _settings.PlotDirectory);
                break;

            case "Change Wallet File Path":
                _settings.WalletFilePath = AnsiConsole.Ask<string>(
                    "Enter wallet file path:",
                    _settings.WalletFilePath);
                break;

            case "Change Miner Config Path":
                _settings.MinerConfigPath = AnsiConsole.Ask<string>(
                    "Enter miner config path:",
                    _settings.MinerConfigPath);
                break;

            case "Save Settings":
                SaveConfiguration();
                break;
        }
    }
}

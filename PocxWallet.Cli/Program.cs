using PocxWallet.Cli.Commands;
using PocxWallet.Cli.Configuration;
using PocxWallet.Cli.Resources;
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
    private static string MenuWallet = $"{Markup.Escape(Strings.MainMenu.WalletLabel).PadRight(15)} {Strings.MainMenu.WalletDescription}";
    private static string MenuExit   = $"{Markup.Escape(Strings.MainMenu.ExitLabel).PadRight(15)} {Strings.MainMenu.ExitDescription}";

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

        // Load service definitions from services.yaml (replaces appsettings.json)
        _serviceConfig = ServiceDefinitionLoader.LoadServices();
        var dynamicMenuBuilder = GetDynamicMenuBuilder();
        var dynamicServices = dynamicMenuBuilder.GetEnabledServices();

        // Main menu loop
        bool exit = false;
        while (!exit)
        {
            // Build dynamic main menu choices
            var menuChoices = new List<string>();
            
            // Add hardcoded items first (Wallet only - Vanity is now inside Wallet -> Create)
            menuChoices.Add(MenuWallet);
            
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
                    .Title(Strings.MainMenu.Title)
                    .PageSize(15)
                    .AddChoices(menuChoices)
            );

            AnsiConsole.Clear();
            ShowBanner();

            // Handle menu choice
            if (choice == MenuWallet)
            {
                // Pass node status check and exec functions for wallet commands
                var dockerManager = GetDockerManager();
                var dynamicBuilder = GetDynamicMenuBuilder();
                
                // Get the Bitcoin node service definition for proper container name and start functionality
                var bitcoinNodeService = dynamicServices.FirstOrDefault(s => s.Id == "bitcoin-node");
                var bitcoinContainerName = bitcoinNodeService != null 
                    ? dynamicBuilder.GetContainerName(bitcoinNodeService)
                    : _settings.BitcoinContainerName;
                
                Func<Task<bool>> isNodeRunning = async () =>
                {
                    var status = await dockerManager.GetContainerStatusAsync(bitcoinContainerName);
                    return status == "running";
                };
                
                Func<string, Task<(int, string)>> execInContainer = async (command) =>
                {
                    return await dockerManager.ExecInContainerAsync(bitcoinContainerName, command);
                };
                
                Func<Task<bool>> startNodeService = async () =>
                {
                    if (bitcoinNodeService != null)
                    {
                        await dynamicBuilder.StartServiceAsync(bitcoinNodeService);
                        // Wait for the node container to fully start (startup delay)
                        const int NodeStartupDelayMs = 2000;
                        await Task.Delay(NodeStartupDelayMs);
                        var status = await dockerManager.GetContainerStatusAsync(bitcoinContainerName);
                        return status == "running";
                    }
                    return false;
                };
                
                await WalletCommands.ShowWalletMenuAsync(ShowBanner, bitcoinContainerName, isNodeRunning, execInContainer, startNodeService);
            }
            else if (choice == MenuExit)
            {
                exit = true;
                AnsiConsole.MarkupLine(Strings.MainMenu.Goodbye);
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
                //AnsiConsole.MarkupLine(Strings.MainMenu.PressEnterToReturn);
                //Console.ReadLine();

                // Show background services status
                if (BackgroundServiceManager.HasRunningServices())
                {
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine(Strings.MainMenu.BackgroundServicesHeader);
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
        var rule = new Rule(Strings.Banner.Title);
        var ruleLine = new Rule();
        ruleLine.RuleStyle("blue dim");
        rule.RuleStyle("blue dim");
        rule.LeftJustified();
        AnsiConsole.Write(ruleLine);
        AnsiConsole.Write(rule);
        AnsiConsole.Write(ruleLine);
        AnsiConsole.WriteLine();
    }
}

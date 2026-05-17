using PocxWallet.Cli.Commands;
using PocxWallet.Cli.Configuration;
using PocxWallet.Cli.Resources;
using PocxWallet.Cli.Services;
using Spectre.Console;
using System;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading.Tasks;

namespace PocxWallet.Cli;

class Program
{
    private static DockerServiceManager? _dockerManager;
    private static ServiceConfiguration? _serviceConfig;
    private static DynamicServiceMenuBuilder? _dynamicMenuBuilder;

    // Constants for hardcoded menu items
    private static string MenuWallet = $"{Markup.Escape(Strings.MainMenu.WalletLabel).PadRight(15)} {Strings.MainMenu.WalletDescription}";
    private static string MenuExit   = $"{Markup.Escape(Strings.MainMenu.ExitLabel).PadRight(15)} {Strings.MainMenu.ExitDescription}";

    // Readonly instance fields initialized in the constructor
    private static readonly string? _version;
    private static readonly string? _product;

    static Program()
    {
        var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var raw = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        string? version = null;
        if (!string.IsNullOrWhiteSpace(raw))
        {
            int i = raw.IndexOf('+');
            version = i > -1 ? raw.Substring(0, i).Trim() : raw.Trim();
        }
        _version = version;
        _product = asm.GetCustomAttribute<AssemblyProductAttribute>()?.Product;
    }

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
            _dynamicMenuBuilder = new DynamicServiceMenuBuilder(_serviceConfig, GetDockerManager());
        }
        return _dynamicMenuBuilder;
    }

    static async Task Main(string[] args)
    {
        // Set UTF-8 encoding for better compatibility
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;

        // Display banner
        ShowBanner();

        // Load service definitions from services.yaml (replaces appsettings.json)
        _serviceConfig = ServiceDefinitionLoader.LoadServices();

        // Main menu loop
        bool exit = false;
        while (!exit)
        {
            // Reload service configuration to ensure settings (network, RPC, etc.) are always fresh
            _serviceConfig = ServiceDefinitionLoader.LoadServices();
            var dynamicMenuBuilder = GetDynamicMenuBuilder();
            var dynamicServices = dynamicMenuBuilder.GetEnabledServices();

            // Build dynamic main menu choices
            var menuChoices = new List<string>();
            
            // Add hardcoded items first (Wallet only - Vanity is now inside Wallet -> Create)
            menuChoices.Add(MenuWallet);
            
            // Add dynamic services from services.yaml (sorted by menu order)
            var serviceStatusMap = new Dictionary<string, string>();
            foreach (var service in dynamicServices)
            {
                var status = await dynamicMenuBuilder.GetServiceStatusIndicatorAsync(service);
                var label = $"{Markup.Escape(service.MenuLabel).PadRight(15)} {service.Name.PadRight(20)} {status}";
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
                var mode = bitcoinNodeService?.GetExecutionMode() ?? ExecutionMode.Docker;
                var bitcoinContainerName = bitcoinNodeService != null 
                    ? dynamicBuilder.GetContainerName(bitcoinNodeService)
                    : "pocx-node";  // Default fallback
                
                Func<Task<bool>> isNodeRunning = async () =>
                {
                    if (mode == ExecutionMode.External && bitcoinNodeService != null)
                    {
                        // For external node, "running" means RPC is reachable
                        return await dynamicBuilder.IsServiceRunningAsync(bitcoinNodeService);
                    }
                    var status = await dockerManager.GetContainerStatusAsync(bitcoinContainerName);
                    return status == "running";
                };
                
                Func<string, Task<(int, string)>> execOnNode = async (command) =>
                {
                    // Find bitcoin-cli binary path
                    var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    var nodeBinDir = Path.Combine(baseDir, "bitcoin-node");
                    var cliName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "bitcoin-cli.exe" : "bitcoin-cli";
                    var cliPath = Path.Combine(nodeBinDir, cliName);

                    if (!File.Exists(cliPath))
                    {
                        // Fallback to searching in PATH if not in local bitcoin-node folder
                        cliPath = cliName;
                    }

                    if (mode == ExecutionMode.External && bitcoinNodeService != null)
                    {
                        // For external node, execute via local bitcoin-cli with RPC parameters from the service's Parameters
                        var parameters = bitcoinNodeService.GetActiveParameters();
                        var host = parameters?.FirstOrDefault(p => p.Name == "rpcconnect")?.Value?.ToString() ?? "127.0.0.1";
                        var port = parameters?.FirstOrDefault(p => p.Name == "rpcport")?.Value?.ToString() ?? "8332";
                        var user = parameters?.FirstOrDefault(p => p.Name == "rpcuser")?.Value?.ToString() ?? "";
                        var password = parameters?.FirstOrDefault(p => p.Name == "rpcpassword")?.Value?.ToString() ?? "";

                        var rpcArgs = $"-rpcconnect={host} -rpcport={port} ";
                        if (!string.IsNullOrEmpty(user)) rpcArgs += $"-rpcuser={user} ";
                        if (!string.IsNullOrEmpty(password)) rpcArgs += $"-rpcpassword={password} ";

                        // Command might already start with "bitcoin-cli"
                        var cleanCommand = command.Trim();
                        if (cleanCommand.StartsWith("bitcoin-cli ")) 
                            cleanCommand = cleanCommand.Substring(12);

                        // Execute locally
                        try
                        {
                            var psi = new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = cliPath,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            };

                            // Split rpcArgs and cleanCommand but respect quotes
                            var fullCommandLine = $"{rpcArgs}{cleanCommand}";
                            var args = PocxWallet.Cli.Services.CommandLineHelper.SplitCommandLineArguments(fullCommandLine);
                            foreach (var arg in args)
                            {
                                psi.ArgumentList.Add(arg);
                            }

                            var process = new System.Diagnostics.Process { StartInfo = psi };
                            process.Start();
                            var output = await process.StandardOutput.ReadToEndAsync();
                            var error = await process.StandardError.ReadToEndAsync();
                            await process.WaitForExitAsync();
                            return (process.ExitCode, string.IsNullOrEmpty(output) ? error : output);
                        }
                        catch (Exception ex)
                        {
                            return (1, string.Format(Strings.ExternalNode.RpcErrorFormat, ex.Message));
                        }
                    }
                    return await dockerManager.ExecInContainerAsync(bitcoinContainerName, command);
                };
                
                Func<Task<bool>> startNodeService = async () =>
                {
                    if (mode == ExecutionMode.External)
                    {
                        // External nodes cannot be started by the wallet
                        AnsiConsole.MarkupLine(Strings.ExternalNode.ManualStart);
                        return await isNodeRunning();
                    }
                    if (bitcoinNodeService != null)
                    {
                        await dynamicBuilder.StartServiceAsync(bitcoinNodeService);
                        // Wait for the node container to fully start (startup delay)
                        const int NodeStartupDelayMs = 2000;
                        await Task.Delay(NodeStartupDelayMs);
                        return await isNodeRunning();
                    }
                    return false;
                };
                
                await WalletCommands.ShowWalletMenuAsync(ShowBanner, bitcoinContainerName, isNodeRunning, execOnNode, startNodeService);
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
        var rule = new Rule($"[bold red]{_product}{(string.IsNullOrWhiteSpace(_version) ? "" : " v" + _version)}[/]");
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

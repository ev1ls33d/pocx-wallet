using PocxWallet.Protocol.Wrappers;
using PocxWallet.Cli.Services;
using PocxWallet.Cli.Configuration;
using Spectre.Console;

namespace PocxWallet.Cli.Commands;

/// <summary>
/// Bitcoin-PoCX node commands
/// </summary>
public static class NodeCommands
{
    private static BitcoinNodeWrapper? _activeNode;
    private static BitcoinCliWrapper? _cliWrapper;
    private const string SERVICE_ID = "bitcoin-node";
    private static DockerServiceManager? _dockerManager;

    private static DockerServiceManager GetDockerManager(AppSettings settings)
    {
        if (_dockerManager == null)
        {
            _dockerManager = new DockerServiceManager(settings.DockerRegistry, settings.DockerImageTag);
        }
        return _dockerManager;
    }

    public static async Task StartNodeAsync(AppSettings settings, string? dataDir = null)
    {
        // Use Docker by default
        if (settings.UseDocker)
        {
            await StartNodeDockerAsync(settings, dataDir);
        }
        else
        {
            StartNodeNative(settings.BitcoinBinariesPath, dataDir, settings.BitcoinNodePort);
        }
    }

    private static async Task StartNodeDockerAsync(AppSettings settings, string? dataDir = null)
    {
        var docker = GetDockerManager(settings);

        if (!await docker.IsDockerAvailableAsync())
        {
            AnsiConsole.MarkupLine("[red]Docker is not available.[/]");
            AnsiConsole.MarkupLine("[dim]Install Docker or disable Docker mode in Settings[/]");
            return;
        }

        // Ensure Docker network exists
        await docker.EnsureNetworkExistsAsync(settings.DockerNetwork);

        // Check if container is already running
        var status = await docker.GetContainerStatusAsync(settings.BitcoinContainerName);
        if (status == "running")
        {
            AnsiConsole.MarkupLine("[yellow]Bitcoin node container is already running![/]");
            
            // Check if electrs should also be started
            if (settings.EnableElectrs)
            {
                var electrsStatus = await docker.GetContainerStatusAsync(settings.ElectrsContainerName);
                if (electrsStatus != "running")
                {
                    await StartElectrsDockerAsync(settings, dataDir);
                }
            }
            return;
        }

        if (string.IsNullOrWhiteSpace(dataDir))
        {
            dataDir = "./bitcoin-data";
        }

        // Create data directory if it doesn't exist
        if (!Directory.Exists(dataDir))
        {
            Directory.CreateDirectory(dataDir);
        }

        var absoluteDataDir = Path.GetFullPath(dataDir);

        var volumeMounts = new Dictionary<string, string>
        {
            { absoluteDataDir, "/root/.bitcoin" }
        };

        // Don't expose ports on node if electrs will handle it
        Dictionary<int, int>? portMappings = settings.EnableElectrs ? null : new Dictionary<int, int>
        {
            { settings.BitcoinNodePort, 18332 },
            { 18333, 18333 }  // P2P port
        };

        AnsiConsole.MarkupLine("[bold]Starting Bitcoin node...[/]");
        
        var success = await docker.StartContainerAsync(
            settings.BitcoinContainerName,
            "bitcoin",
            volumeMounts: volumeMounts,
            portMappings: portMappings,
            command: "bitcoind -printtoconsole -rpcport=18332 -rpcallowip=0.0.0.0/0 -rpcbind=0.0.0.0",
            network: settings.DockerNetwork
        );

        if (success)
        {
            // Register with background service manager
            BackgroundServiceManager.RegisterService(
                settings.BitcoinContainerName,
                "Bitcoin Node (Docker)"
            );
            
            // Start electrs if enabled
            if (settings.EnableElectrs)
            {
                // Wait a moment for node to initialize
                await Task.Delay(2000);
                await StartElectrsDockerAsync(settings, dataDir);
            }
        }
    }

    private static async Task StartElectrsDockerAsync(AppSettings settings, string? bitcoinDataDir = null)
    {
        const int ElectrsHttpPort = 3000;
        const int ElectrsRpcPort = 50001;
        const int ElectrsTestnetPort = 60001;

        var docker = GetDockerManager(settings);

        // Check if electrs is already running
        var status = await docker.GetContainerStatusAsync(settings.ElectrsContainerName);
        if (status == "running")
        {
            AnsiConsole.MarkupLine("[yellow]Electrs container is already running![/]");
            return;
        }

        var electrsDataDir = "./electrs-data";
        if (!Directory.Exists(electrsDataDir))
        {
            Directory.CreateDirectory(electrsDataDir);
        }

        var absoluteElectrsDataDir = Path.GetFullPath(electrsDataDir);
        
        if (string.IsNullOrWhiteSpace(bitcoinDataDir))
        {
            bitcoinDataDir = "./bitcoin-data";
        }
        var absoluteBitcoinDataDir = Path.GetFullPath(bitcoinDataDir);

        var volumeMounts = new Dictionary<string, string>
        {
            { absoluteElectrsDataDir, "/data" },
            { absoluteBitcoinDataDir, "/root/.bitcoin" }
        };

        var portMappings = new Dictionary<int, int>
        {
            { ElectrsHttpPort, ElectrsHttpPort },
            { ElectrsRpcPort, ElectrsRpcPort },
            { ElectrsTestnetPort, ElectrsTestnetPort }
        };

        AnsiConsole.MarkupLine("[bold]Starting Electrs server...[/]");

        var success = await docker.StartContainerAsync(
            settings.ElectrsContainerName,
            "electrs",
            volumeMounts: volumeMounts,
            portMappings: portMappings,
            command: $"electrs --http-addr 0.0.0.0:{ElectrsHttpPort} --electrum-rpc-addr 0.0.0.0:{ElectrsRpcPort} --daemon-rpc-addr {settings.BitcoinContainerName}:18332 --daemon-dir /root/.bitcoin --db-dir /data",
            network: settings.DockerNetwork
        );

        if (success)
        {
            BackgroundServiceManager.RegisterService(
                settings.ElectrsContainerName,
                "Electrs Server (Docker)"
            );
            AnsiConsole.MarkupLine("[green]✓[/] Electrs server started successfully");
            AnsiConsole.MarkupLine($"[dim]HTTP API: http://localhost:{ElectrsHttpPort}[/]");
            AnsiConsole.MarkupLine($"[dim]Electrum RPC: localhost:{ElectrsRpcPort}[/]");
            AnsiConsole.MarkupLine($"[dim]Testnet RPC: localhost:{ElectrsTestnetPort}[/]");
        }
    }

    private static void StartNodeNative(string binariesPath, string? dataDir = null, int rpcPort = 18332)
    {
        if (_activeNode?.IsRunning == true)
        {
            AnsiConsole.MarkupLine("[yellow]Bitcoin-PoCX node is already running![/]");
            return;
        }

        var nodePath = Path.Combine(binariesPath, "bitcoind");
        if (!File.Exists(nodePath))
        {
            AnsiConsole.MarkupLine($"[red]bitcoind binary not found at: {nodePath}[/]");
            AnsiConsole.MarkupLine("[dim]Make sure Bitcoin-PoCX is built in the bitcoin-pocx directory[/]");
            return;
        }

        try
        {
            _activeNode = new BitcoinNodeWrapper(nodePath);

            AnsiConsole.MarkupLine("[bold green]Starting Bitcoin-PoCX node as background service...[/]");
            AnsiConsole.MarkupLine($"[dim]RPC Port: {rpcPort}[/]");
            if (!string.IsNullOrEmpty(dataDir))
            {
                AnsiConsole.MarkupLine($"[dim]Data Directory: {dataDir}[/]");
            }
            AnsiConsole.WriteLine();

            _activeNode.StartNode(
                dataDir,
                rpcPort,
                onOutput: output => { }, // Silent in background
                onError: error => { });

            // Initialize CLI wrapper
            var cliPath = Path.Combine(binariesPath, "bitcoin-cli");
            if (File.Exists(cliPath))
            {
                _cliWrapper = new BitcoinCliWrapper(cliPath, rpcPort);
            }

            // Register as background service
            BackgroundServiceManager.RegisterService(SERVICE_ID, "Bitcoin-PoCX Node");

            AnsiConsole.MarkupLine("[green]√[/] Bitcoin-PoCX node started as background service!");
            AnsiConsole.MarkupLine("[dim]Check 'Background Services' section in main menu[/]");
            AnsiConsole.MarkupLine($"[dim]RPC available at localhost:{rpcPort}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }

    public static async Task StopNodeAsync(AppSettings settings)
    {
        // Use Docker by default
        if (settings.UseDocker)
        {
            await StopNodeDockerAsync(settings);
        }
        else
        {
            StopNodeNative();
        }
    }

    private static async Task StopNodeDockerAsync(AppSettings settings)
    {
        var docker = GetDockerManager(settings);
        
        // Stop electrs if it's running
        if (settings.EnableElectrs)
        {
            var electrsStatus = await docker.GetContainerStatusAsync(settings.ElectrsContainerName);
            if (electrsStatus == "running")
            {
                AnsiConsole.MarkupLine("[bold]Stopping Electrs server...[/]");
                await docker.StopContainerAsync(settings.ElectrsContainerName);
                BackgroundServiceManager.RemoveService(settings.ElectrsContainerName);
            }
        }
        
        // Stop node
        AnsiConsole.MarkupLine("[bold]Stopping Bitcoin node...[/]");
        await docker.StopContainerAsync(settings.BitcoinContainerName);
        BackgroundServiceManager.RemoveService(settings.BitcoinContainerName);
    }

    private static void StopNodeNative()
    {
        if (_activeNode?.IsRunning != true)
        {
            AnsiConsole.MarkupLine("[yellow]No active node to stop[/]");
            return;
        }

        AnsiConsole.Status()
            .Start("Stopping Bitcoin-PoCX node...", ctx =>
            {
                try
                {
                    // Try graceful shutdown via CLI if available
                    if (_cliWrapper != null)
                    {
                        var stopTask = _cliWrapper.StopNodeAsync();
                        stopTask.Wait(TimeSpan.FromSeconds(5));
                    }
                }
                catch { }

                _activeNode.StopProcess();
                _activeNode.Dispose();
                _activeNode = null;
                _cliWrapper = null;

                // Remove from background services
                BackgroundServiceManager.RemoveService(SERVICE_ID);
            });

        AnsiConsole.MarkupLine("[green]√[/] Bitcoin-PoCX node stopped");
    }

    public static async Task ShowNodeStatusAsync(AppSettings settings)
    {
        if (settings.UseDocker)
        {
            await ShowNodeStatusDockerAsync(settings);
        }
        else
        {
            ShowNodeStatusNative();
        }
    }

    public static async Task ViewLogsAsync(AppSettings settings)
    {
        if (settings.UseDocker)
        {
            var docker = GetDockerManager(settings);
            
            var containerChoice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select [green]container[/] to view logs:")
                    .AddChoices(new[] { 
                        settings.BitcoinContainerName, 
                        settings.ElectrsContainerName 
                    }));

            var lines = AnsiConsole.Ask("How many log lines to display?", 50);
            await docker.DisplayContainerLogsAsync(containerChoice, lines, $"{containerChoice} Logs");
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]Log viewing is only available in Docker mode[/]");
            AnsiConsole.MarkupLine("[dim]Enable Docker mode in Settings to use this feature[/]");
        }
    }

    private static async Task ShowNodeStatusDockerAsync(AppSettings settings)
    {
        var docker = GetDockerManager(settings);
        var status = await docker.GetContainerStatusAsync(settings.BitcoinContainerName);

        if (status == "not found")
        {
            AnsiConsole.MarkupLine("[dim]Bitcoin-PoCX node container is not running[/]");
            return;
        }

        var statusColor = status == "running" ? "green" : "yellow";
        AnsiConsole.MarkupLine($"[{statusColor}]Container status: {status}[/]");

        if (status == "running")
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Recent logs:[/]");
            var logs = await docker.GetContainerLogsAsync(settings.BitcoinContainerName, 10);
            
            var panel = new Panel(logs.Length > 500 ? logs.Substring(logs.Length - 500) : logs)
            {
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Green)
            };
            
            AnsiConsole.Write(panel);
        }
    }

    private static void ShowNodeStatusNative()
    {
        if (_activeNode?.IsRunning != true)
        {
            AnsiConsole.MarkupLine("[dim]Bitcoin-PoCX node is not running[/]");
            return;
        }

        AnsiConsole.MarkupLine("[green]√[/] Bitcoin-PoCX node is running");

        if (_cliWrapper != null)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Status()
                .Start("Fetching node info...", ctx =>
                {
                    try
                    {
                        var infoTask = _cliWrapper.GetBlockchainInfoAsync();
                        infoTask.Wait(TimeSpan.FromSeconds(5));
                        
                        if (infoTask.IsCompletedSuccessfully)
                        {
                            AnsiConsole.WriteLine();
                            AnsiConsole.MarkupLine("[bold]Blockchain Info:[/]");
                            AnsiConsole.WriteLine(infoTask.Result);
                        }
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[yellow]Could not fetch info: {ex.Message}[/]");
                        AnsiConsole.MarkupLine("[dim]Node may still be starting up...[/]");
                    }
                });
        }
    }

    public static async Task CheckAddressBalanceAsync(string address)
    {
        if (_cliWrapper == null)
        {
            AnsiConsole.MarkupLine("[yellow]Bitcoin-PoCX node is not running[/]");
            AnsiConsole.MarkupLine("[dim]Start the node first from the Node menu[/]");
            return;
        }

        try
        {
            AnsiConsole.MarkupLine($"[bold]Checking balance for:[/] [green]{address}[/]");
            
            await AnsiConsole.Status()
                .StartAsync("Querying node...", async ctx =>
                {
                    var balance = await _cliWrapper.GetBalanceAsync();
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine($"[bold]Balance:[/] {balance}");
                });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }

    public static async Task SendTransactionAsync(string toAddress, decimal amount)
    {
        if (_cliWrapper == null)
        {
            AnsiConsole.MarkupLine("[yellow]Bitcoin-PoCX node is not running[/]");
            AnsiConsole.MarkupLine("[dim]Start the node first from the Node menu[/]");
            return;
        }

        try
        {
            if (!AnsiConsole.Confirm($"Send {amount} PoCX to {toAddress}?", false))
            {
                AnsiConsole.MarkupLine("[yellow]Transaction cancelled[/]");
                return;
            }

            await AnsiConsole.Status()
                .StartAsync("Broadcasting transaction...", async ctx =>
                {
                    var txid = await _cliWrapper.SendToAddressAsync(toAddress, amount);
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("[green]√[/] Transaction sent!");
                    AnsiConsole.MarkupLine($"[bold]Transaction ID:[/] {txid}");
                });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }

    public static BitcoinCliWrapper? GetCliWrapper()
    {
        return _cliWrapper;
    }

    public static bool IsNodeRunning()
    {
        return _activeNode?.IsRunning == true;
    }
}

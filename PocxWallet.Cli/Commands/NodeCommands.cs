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
    private const string SERVICE_ID = "bitcoin-node";
    private static DockerServiceManager? _dockerManager;

    private static DockerServiceManager GetDockerManager()
    {
        if (_dockerManager == null)
        {
            _dockerManager = new DockerServiceManager();
        }
        return _dockerManager;
    }

    public static async Task StartNodeAsync(AppSettings settings, string? dataDir = null)
    {
        await StartNodeDockerAsync(settings, dataDir);
    }

    private static async Task StartNodeDockerAsync(AppSettings settings, string? dataDir = null)
    {
        var docker = GetDockerManager();

        if (!await docker.IsDockerAvailableAsync())
        {
            AnsiConsole.MarkupLine("[red]Docker is not available.[/]");
            AnsiConsole.MarkupLine("[dim]Install Docker using the Docker setup menu option[/]");
            return;
        }

        // Ensure Docker network exists
        await docker.EnsureNetworkExistsAsync(settings.DockerNetwork);

        // Use configured data directory or default
        if (string.IsNullOrWhiteSpace(dataDir))
        {
            dataDir = settings.BitcoinNode.DataDirectory;
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

        // Build environment variables from settings
        var envVars = new Dictionary<string, string>(settings.BitcoinNode.EnvironmentVariables);

        var portMappings = new Dictionary<int, int>
        {
            { settings.BitcoinNode.RpcPort, 18332 },
            { settings.BitcoinNode.P2PPort, 18333 }  // P2P port
        };

        AnsiConsole.MarkupLine("[bold]Starting Bitcoin node...[/]");
        
        var command = $"bitcoind {settings.BitcoinNode.AdditionalParams}";
        
        var success = await docker.StartContainerAsync(
            settings.BitcoinContainerName,
            "bitcoin",
            settings.BitcoinNode.Repository,
            settings.BitcoinNode.Tag,
            environmentVars: envVars.Count > 0 ? envVars : null,
            volumeMounts: volumeMounts,
            portMappings: portMappings,
            command: command,
            network: settings.DockerNetwork
        );

        if (success)
        {
            // Register with background service manager
            BackgroundServiceManager.RegisterService(
                settings.BitcoinContainerName,
                "Bitcoin Node (Docker)"
            );
        }
    }

    public static async Task StopNodeAsync(AppSettings settings)
    {
        await StopNodeDockerAsync(settings);
    }

    private static async Task StopNodeDockerAsync(AppSettings settings)
    {
        var docker = GetDockerManager();
        
        // Stop node
        AnsiConsole.MarkupLine("[bold]Stopping Bitcoin node...[/]");
        await docker.StopContainerAsync(settings.BitcoinContainerName);
        BackgroundServiceManager.RemoveService(settings.BitcoinContainerName);
    }

    public static async Task ShowNodeStatusAsync(AppSettings settings)
    {
        await ShowNodeStatusDockerAsync(settings);
    }

    public static async Task ViewLogsAsync(AppSettings settings)
    {
        var docker = GetDockerManager();
        var lines = AnsiConsole.Ask("How many log lines to display?", 50);
        await docker.DisplayContainerLogsAsync(settings.BitcoinContainerName, lines, "Bitcoin Node Logs");
    }

    private static async Task ShowNodeStatusDockerAsync(AppSettings settings)
    {
        var docker = GetDockerManager();
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

    /// <summary>
    /// Execute bitcoin-cli command in Docker container
    /// </summary>
    public static async Task<string> ExecuteBitcoinCliDockerAsync(AppSettings settings, string command, params string[] args)
    {
        var docker = GetDockerManager();
        var allArgs = string.Join(" ", args);
        var fullCommand = $"bitcoin-cli -rpcport=18332 {command} {allArgs}";
        var result = await docker.ExecInContainerAsync(settings.BitcoinContainerName, fullCommand);
        return result.output;
    }

    /// <summary>
    /// Check if wallet exists and load it if necessary
    /// </summary>
    public static async Task<bool> EnsureWalletLoadedAsync(AppSettings settings, string walletName)
    {
        try
        {
            // Check if wallet is already loaded
            var loadedWallets = await ExecuteBitcoinCliDockerAsync(settings, "listwallets");

            if (loadedWallets.Contains($"\"{walletName}\""))
            {
                return true; // Wallet already loaded
            }

            // Check if wallet directory exists
            var walletDir = await ExecuteBitcoinCliDockerAsync(settings, "listwalletdir");

            if (walletDir.Contains($"\"{walletName}\""))
            {
                // Wallet exists, try to load it
                var loadResult = await ExecuteBitcoinCliDockerAsync(settings, "loadwallet", walletName, "true");

                return !loadResult.Contains("error");
            }

            return false; // Wallet doesn't exist
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Import wallet from WIF into bitcoin node
    /// </summary>
    public static async Task<bool> ImportWalletFromWIFAsync(AppSettings settings, string walletName, string wif, string address, bool testnet = false)
    {
        try
        {
            // First, ensure wallet exists or create it
            var walletLoaded = await EnsureWalletLoadedAsync(settings, walletName);
            
            if (!walletLoaded)
            {
                // Create new wallet
                AnsiConsole.MarkupLine($"[yellow]Creating wallet '{walletName}'...[/]");
                var createResult = await ExecuteBitcoinCliDockerAsync(settings, "createwallet", walletName, "false", "false", "\"\"", "false", "true", "true");

                if (createResult.Contains("error"))
                {
                    AnsiConsole.MarkupLine($"[red]Failed to create wallet:[/] {createResult}");
                    return false;
                }
                AnsiConsole.MarkupLine("[green]✓[/] Wallet created");
            }

            // Check if address already exists in wallet
            var receivedByAddress = await ExecuteBitcoinCliDockerAsync(settings, "listreceivedbyaddress", "0", "true");

            if (receivedByAddress.Contains(address))
            {
                AnsiConsole.MarkupLine("[yellow]Address already exists in wallet[/]");
                return true;
            }

            // Import the descriptor
            AnsiConsole.MarkupLine("[yellow]Importing WIF into wallet...[/]");
            
            // Build the descriptor JSON for import using proper JSON serialization
            var descriptor = new[]
            {
                new
                {
                    desc = wif,
                    timestamp = "now",
                }
            };
            var descriptorJson = System.Text.Json.JsonSerializer.Serialize(descriptor);
            
            var importResult = await ExecuteBitcoinCliDockerAsync(settings, "importdescriptors", $"'{descriptorJson}'");

            if (importResult.Contains("\"success\": true") || importResult.Contains("\"success\":true"))
            {
                AnsiConsole.MarkupLine("[green]✓[/] WIF imported successfully");
                return true;
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Failed to import WIF:[/] {Markup.Escape(importResult)}");
                return false;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error importing wallet:[/] {ex.Message}");
            return false;
        }
    }
}

using PocxWallet.Cli.Services;
using PocxWallet.Cli.Configuration;
using Spectre.Console;

namespace PocxWallet.Cli.Commands;

/// <summary>
/// Electrs (Electrum server) commands
/// </summary>
public static class ElectrsCommands
{
    private const string SERVICE_ID = "electrs";
    private static DockerServiceManager? _dockerManager;

    private static DockerServiceManager GetDockerManager()
    {
        if (_dockerManager == null)
        {
            _dockerManager = new DockerServiceManager();
        }
        return _dockerManager;
    }

    public static async Task StartElectrsAsync(AppSettings settings)
    {
        var docker = GetDockerManager();

        if (!await docker.IsDockerAvailableAsync())
        {
            AnsiConsole.MarkupLine("[red]Docker is not available.[/]");
            AnsiConsole.MarkupLine("[dim]Install Docker using the Docker setup menu option[/]");
            return;
        }

        // Check if Bitcoin node is running
        var nodeStatus = await docker.GetContainerStatusAsync(settings.BitcoinContainerName);
        if (nodeStatus != "running")
        {
            AnsiConsole.MarkupLine("[red]Bitcoin node must be running before starting Electrs![/]");
            AnsiConsole.MarkupLine("[dim]Start the Bitcoin node first from the Node menu[/]");
            return;
        }

        // Ensure Docker network exists
        await docker.EnsureNetworkExistsAsync(settings.DockerNetwork);

        var electrsDataDir = settings.Electrs.DataDirectory;
        if (!Directory.Exists(electrsDataDir))
        {
            Directory.CreateDirectory(electrsDataDir);
        }

        var absoluteElectrsDataDir = Path.GetFullPath(electrsDataDir);
        var bitcoinDataDir = settings.BitcoinNode.DataDirectory;
        var absoluteBitcoinDataDir = Path.GetFullPath(bitcoinDataDir);

        // Volume mounts: electrs needs read-only access to bitcoin's .cookie file
        var volumeMounts = new Dictionary<string, string>
        {
            { absoluteElectrsDataDir, "/data" },
            { absoluteBitcoinDataDir, "/root/.bitcoin" }
        };

        // Mark bitcoin data directory as read-only (for .cookie file access)
        var readOnlyVolumes = new List<string> { absoluteBitcoinDataDir };

        var portMappings = new Dictionary<int, int>
        {
            { settings.Electrs.HttpPort, settings.Electrs.HttpPort },
            { settings.Electrs.RpcPort, settings.Electrs.RpcPort },
            { settings.Electrs.TestnetPort, settings.Electrs.TestnetPort }
        };

        // Build environment variables from settings
        var envVars = new Dictionary<string, string>(settings.Electrs.EnvironmentVariables);

        AnsiConsole.MarkupLine("[bold]Starting Electrs server...[/]");

        var command = $"electrs --http-addr 0.0.0.0:{settings.Electrs.HttpPort} --electrum-rpc-addr 0.0.0.0:{settings.Electrs.RpcPort} --daemon-rpc-addr {settings.BitcoinContainerName}:18332 --daemon-dir /root/.bitcoin --db-dir /data {settings.Electrs.AdditionalParams}";

        var success = await docker.StartContainerAsync(
            settings.ElectrsContainerName,
            "electrs",
            settings.Electrs.Repository,
            settings.Electrs.Tag,
            environmentVars: envVars.Count > 0 ? envVars : null,
            volumeMounts: volumeMounts,
            portMappings: portMappings,
            command: command,
            network: settings.DockerNetwork,
            readOnlyVolumes: readOnlyVolumes
        );

        if (success)
        {
            BackgroundServiceManager.RegisterService(
                settings.ElectrsContainerName,
                "Electrs Server (Docker)"
            );
            AnsiConsole.MarkupLine("[green]✓[/] Electrs server started successfully");
            AnsiConsole.MarkupLine($"[dim]HTTP API: http://localhost:{settings.Electrs.HttpPort}[/]");
            AnsiConsole.MarkupLine($"[dim]Electrum RPC: localhost:{settings.Electrs.RpcPort}[/]");
            AnsiConsole.MarkupLine($"[dim]Testnet RPC: localhost:{settings.Electrs.TestnetPort}[/]");
        }
    }

    public static async Task StopElectrsAsync(AppSettings settings)
    {
        var docker = GetDockerManager();
        
        AnsiConsole.MarkupLine("[bold]Stopping Electrs server...[/]");
        await docker.StopContainerAsync(settings.ElectrsContainerName);
        BackgroundServiceManager.RemoveService(settings.ElectrsContainerName);
    }

    public static async Task ViewLogsAsync(AppSettings settings)
    {
        var docker = GetDockerManager();
        var lines = AnsiConsole.Ask("How many log lines to display?", 50);
        await docker.DisplayContainerLogsAsync(settings.ElectrsContainerName, lines, "Electrs Logs");
    }
}

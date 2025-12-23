using PocxWallet.Protocol.Wrappers;
using PocxWallet.Cli.Services;
using PocxWallet.Cli.Configuration;
using Spectre.Console;
using System.Diagnostics;

namespace PocxWallet.Cli.Commands;

/// <summary>
/// Mining-related commands
/// </summary>
public static class MiningCommands
{
    private const string SERVICE_ID = "miner";
    private static DockerServiceManager? _dockerManager;

    private static DockerServiceManager GetDockerManager()
    {
        if (_dockerManager == null)
        {
            _dockerManager = new DockerServiceManager();
        }
        return _dockerManager;
    }

    public static async Task StartMiningAsync(AppSettings settings)
    {
        await StartMiningDockerAsync(settings);
    }

    private static async Task StartMiningDockerAsync(AppSettings settings)
    {
        var docker = GetDockerManager();

        if (!await docker.IsDockerAvailableAsync())
        {
            AnsiConsole.MarkupLine("[red]Docker is not available.[/]");
            AnsiConsole.MarkupLine("[dim]Install Docker using the Docker setup menu option[/]");
            return;
        }

        if (!File.Exists(settings.MinerConfigPath))
        {
            AnsiConsole.MarkupLine($"[red]Config file not found at: {settings.MinerConfigPath}[/]");
            AnsiConsole.MarkupLine("[dim]Create a config.yaml file first.[/]");
            return;
        }

        var plotDir = settings.PlotDirectory;
        var configDir = Path.GetDirectoryName(Path.GetFullPath(settings.MinerConfigPath));
        var configFileName = Path.GetFileName(settings.MinerConfigPath);

        // Create plot directory if it doesn't exist
        if (!Directory.Exists(plotDir))
        {
            Directory.CreateDirectory(plotDir);
        }

        var absolutePlotDir = Path.GetFullPath(plotDir);

        var volumeMounts = new Dictionary<string, string>
        {
            { absolutePlotDir, "/plots" },
            { Path.GetFullPath(configDir ?? "."), "/config" }
        };

        // Build environment variables from settings
        var envVars = new Dictionary<string, string>(settings.Miner.EnvironmentVariables);

        var command = $"pocx_miner -c /config/{configFileName} {settings.Miner.AdditionalParams}";

        var success = await docker.StartContainerAsync(
            settings.MinerContainerName,
            "pocx",
            settings.Miner.Repository,
            settings.Miner.Tag,
            environmentVars: envVars.Count > 0 ? envVars : null,
            volumeMounts: volumeMounts,
            command: command
        );

        if (success)
        {
            // Register with background service manager
            BackgroundServiceManager.RegisterService(
                settings.MinerContainerName,
                "PoCX Miner (Docker)"
            );
        }
    }

    public static async Task StopMiningAsync(AppSettings settings)
    {
        await StopMiningDockerAsync(settings);
    }

    private static async Task StopMiningDockerAsync(AppSettings settings)
    {
        var docker = GetDockerManager();
        
        await docker.StopContainerAsync(settings.MinerContainerName);
        
        BackgroundServiceManager.RemoveService(settings.MinerContainerName);
    }

    public static async Task ShowMiningStatusAsync(AppSettings settings)
    {
        await ShowMiningStatusDockerAsync(settings);
    }

    private static async Task ShowMiningStatusDockerAsync(AppSettings settings)
    {
        var docker = GetDockerManager();
        var status = await docker.GetContainerStatusAsync(settings.MinerContainerName);

        if (status == "not found")
        {
            AnsiConsole.MarkupLine("[dim]Miner container is not running[/]");
            return;
        }

        var statusColor = status == "running" ? "green" : "yellow";
        AnsiConsole.MarkupLine($"[{statusColor}]Container status: {status}[/]");

        if (status == "running")
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Recent logs:[/]");
            var logs = await docker.GetContainerLogsAsync(settings.MinerContainerName, 20);
            
            var panel = new Panel(logs.Length > 1000 ? logs.Substring(logs.Length - 1000) : logs)
            {
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Green)
            };
            
            AnsiConsole.Write(panel);
        }
    }

    public static void CreateMinerConfig(string configPath)
    {
        AnsiConsole.MarkupLine("[bold green]Create miner configuration[/]");

        var poolUrl = AnsiConsole.Ask<string>("Enter [green]pool URL[/]:", "http://pool.example.com:8080");
        var apiPath = AnsiConsole.Ask<string>("Enter [green]API path[/]:", "/pocx");
        var accountId = AnsiConsole.Ask<string>("Enter your [green]account ID[/]:");
        var plotDir = AnsiConsole.Ask<string>("Enter [green]plot directory[/]:", "./plots");
        var threads = AnsiConsole.Ask<int>("Enter [green]CPU threads[/]:", Environment.ProcessorCount);

        var config = $@"# PoCX Miner Configuration
chains:
  - name: ""primary_pool""
    base_url: ""{poolUrl}""
    api_path: ""{apiPath}""
    accounts:
      - account: ""{accountId}""

plot_dirs:
  - ""{plotDir}""

cpu_threads: {threads}
hdd_use_direct_io: true
show_progress: true
";

        File.WriteAllText(configPath, config);
        AnsiConsole.MarkupLine($"[green]√[/] Configuration saved to: {configPath}");
    }

    public static async Task ViewLogsAsync(AppSettings settings)
    {
        var docker = GetDockerManager();
        var lines = AnsiConsole.Ask("How many log lines to display?", 50);
        await docker.DisplayContainerLogsAsync(settings.MinerContainerName, lines, "Miner Logs");
    }
}

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
    private static MinerWrapper? _activeMiner;
    private const string SERVICE_ID = "miner";
    private static DockerServiceManager? _dockerManager;

    private static DockerServiceManager GetDockerManager(AppSettings settings)
    {
        if (_dockerManager == null)
        {
            _dockerManager = new DockerServiceManager(settings.DockerRegistry, settings.DockerImageTag);
        }
        return _dockerManager;
    }

    public static async Task StartMiningAsync(AppSettings settings)
    {
        if (settings.UseDocker)
        {
            await StartMiningDockerAsync(settings);
        }
        else
        {
            StartMiningNative(settings.PoCXBinariesPath, settings.MinerConfigPath);
        }
    }

    private static async Task StartMiningDockerAsync(AppSettings settings)
    {
        var docker = GetDockerManager(settings);

        if (!await docker.IsDockerAvailableAsync())
        {
            AnsiConsole.MarkupLine("[red]Docker is not available.[/]");
            AnsiConsole.MarkupLine("[dim]Install Docker or disable Docker mode in Settings[/]");
            return;
        }

        if (!File.Exists(settings.MinerConfigPath))
        {
            AnsiConsole.MarkupLine($"[red]Config file not found at: {settings.MinerConfigPath}[/]");
            AnsiConsole.MarkupLine("[dim]Create a config.yaml file first.[/]");
            return;
        }

        // Check if container is already running
        var status = await docker.GetContainerStatusAsync(settings.MinerContainerName);
        if (status == "running")
        {
            AnsiConsole.MarkupLine("[yellow]Miner container is already running![/]");
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

        var success = await docker.StartContainerAsync(
            settings.MinerContainerName,
            "pocx",
            volumeMounts: volumeMounts,
            command: $"pocx_miner -c /config/{configFileName}"
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

    private static void StartMiningNative(string binariesPath, string configPath)
    {
        if (_activeMiner?.IsRunning == true)
        {
            AnsiConsole.MarkupLine("[yellow]Miner is already running![/]");
            return;
        }

        if (!File.Exists(configPath))
        {
            AnsiConsole.MarkupLine($"[red]Config file not found at: {configPath}[/]");
            AnsiConsole.MarkupLine("[dim]Create a config.yaml file first.[/]");
            return;
        }

        var minerPath = Path.Combine(binariesPath, "pocx_miner");
        if (!File.Exists(minerPath))
        {
            AnsiConsole.MarkupLine($"[red]Miner binary not found at: {minerPath}[/]");
            return;
        }

        try
        {
            _activeMiner = new MinerWrapper(minerPath);

            AnsiConsole.MarkupLine("[bold green]Starting miner as background service...[/]");
            AnsiConsole.MarkupLine($"[dim]Config: {configPath}[/]");
            AnsiConsole.WriteLine();

            _activeMiner.StartMining(
                configPath,
                onOutput: output => { }, // Silent in background
                onError: error => { });

            // Register as background service
            BackgroundServiceManager.RegisterService(SERVICE_ID, "PoCX Miner");

            AnsiConsole.MarkupLine("[green]√[/] Miner started as background service!");
            AnsiConsole.MarkupLine("[dim]Check 'Background Services' section in main menu[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }

    public static async Task StopMiningAsync(AppSettings settings)
    {
        if (settings.UseDocker)
        {
            await StopMiningDockerAsync(settings);
        }
        else
        {
            StopMiningNative();
        }
    }

    private static async Task StopMiningDockerAsync(AppSettings settings)
    {
        var docker = GetDockerManager(settings);
        
        await docker.StopContainerAsync(settings.MinerContainerName);
        
        BackgroundServiceManager.RemoveService(settings.MinerContainerName);
    }

    private static void StopMiningNative()
    {
        if (_activeMiner?.IsRunning != true)
        {
            AnsiConsole.MarkupLine("[yellow]No active miner to stop[/]");
            return;
        }

        AnsiConsole.Status()
            .Start("Stopping miner...", ctx =>
            {
                _activeMiner.StopProcess();
                _activeMiner.Dispose();
                _activeMiner = null;
                
                // Remove from background services
                BackgroundServiceManager.RemoveService(SERVICE_ID);
            });

        AnsiConsole.MarkupLine("[green]√[/] Miner stopped");
    }

    public static async Task ShowMiningStatusAsync(AppSettings settings)
    {
        if (settings.UseDocker)
        {
            await ShowMiningStatusDockerAsync(settings);
        }
        else
        {
            ShowMiningStatusNative();
        }
    }

    private static async Task ShowMiningStatusDockerAsync(AppSettings settings)
    {
        var docker = GetDockerManager(settings);
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

    private static void ShowMiningStatusNative()
    {
        if (_activeMiner?.IsRunning == true)
        {
            AnsiConsole.MarkupLine("[green]√[/] Miner is running");
        }
        else
        {
            AnsiConsole.MarkupLine("[dim]Miner is not running[/]");
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
        if (settings.UseDocker)
        {
            var docker = GetDockerManager(settings);
            var lines = AnsiConsole.Ask("How many log lines to display?", 50);
            await docker.DisplayContainerLogsAsync(settings.MinerContainerName, lines, "Miner Logs");
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]Log viewing is only available in Docker mode[/]");
            AnsiConsole.MarkupLine("[dim]Enable Docker mode in Settings to use this feature[/]");
        }
    }
}

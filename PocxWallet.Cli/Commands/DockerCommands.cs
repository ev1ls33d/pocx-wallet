using PocxWallet.Cli.Services;
using PocxWallet.Cli.Configuration;
using Spectre.Console;

namespace PocxWallet.Cli.Commands;

/// <summary>
/// Docker container management commands
/// </summary>
public static class DockerCommands
{
    private static DockerServiceManager? _dockerManager;

    private static DockerServiceManager GetDockerManager(AppSettings settings)
    {
        if (_dockerManager == null)
        {
            _dockerManager = new DockerServiceManager(settings.DockerRegistry, settings.DockerImageTag);
        }
        return _dockerManager;
    }

    /// <summary>
    /// Check Docker installation and status
    /// </summary>
    public static async Task CheckDockerStatusAsync(AppSettings settings)
    {
        var docker = GetDockerManager(settings);

        AnsiConsole.MarkupLine("[bold]Checking Docker status...[/]");
        AnsiConsole.WriteLine();

        var available = await docker.IsDockerAvailableAsync();
        if (available)
        {
            AnsiConsole.MarkupLine("[green]✓[/] Docker is installed and running");
            
            // List PoCX containers
            var containers = await docker.ListContainersAsync();
            if (containers.Any())
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[bold]PoCX Containers:[/]");
                
                var table = new Table();
                table.Border(TableBorder.Rounded);
                table.AddColumn("Name");
                table.AddColumn("Status");
                table.AddColumn("Ports");

                foreach (var container in containers)
                {
                    var statusColor = container.Status.Contains("Up") ? "green" : "red";
                    table.AddRow(
                        container.Name,
                        $"[{statusColor}]{container.Status}[/]",
                        container.Ports
                    );
                }

                AnsiConsole.Write(table);
            }
            else
            {
                AnsiConsole.MarkupLine("[dim]No PoCX containers found[/]");
            }
        }
        else
        {
            AnsiConsole.MarkupLine("[red]✗[/] Docker is not available");
        }
    }

    /// <summary>
    /// Setup Docker (install if needed)
    /// </summary>
    public static async Task SetupDockerAsync(AppSettings settings)
    {
        var docker = GetDockerManager(settings);
        
        await docker.EnsureDockerInstalledAsync();
    }

    /// <summary>
    /// Pull Docker images
    /// </summary>
    public static async Task PullImagesAsync(AppSettings settings)
    {
        var docker = GetDockerManager(settings);

        if (!await docker.IsDockerAvailableAsync())
        {
            AnsiConsole.MarkupLine("[red]Docker is not available. Please run Docker Setup first.[/]");
            return;
        }

        AnsiConsole.MarkupLine("[bold green]Pulling Docker images...[/]");
        AnsiConsole.WriteLine();

        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                var bitcoinTask = ctx.AddTask("[green]Bitcoin-PoCX image[/]");
                var pullSuccess = await docker.PullImageAsync("bitcoin-pocx");
                bitcoinTask.Value = 100;

                if (pullSuccess)
                {
                    var pocxTask = ctx.AddTask("[green]PoCX tools image[/]");
                    await docker.PullImageAsync("pocx");
                    pocxTask.Value = 100;

                    var electrsTask = ctx.AddTask("[green]Electrs-PoCX image[/]");
                    await docker.PullImageAsync("electrs-pocx");
                    electrsTask.Value = 100;
                }
            });

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]✓[/] Images pulled successfully");
    }

    /// <summary>
    /// Start Bitcoin-PoCX node container
    /// </summary>
    public static async Task StartBitcoinNodeContainerAsync(AppSettings settings)
    {
        var docker = GetDockerManager(settings);

        if (!await docker.IsDockerAvailableAsync())
        {
            AnsiConsole.MarkupLine("[red]Docker is not available. Please run Docker Setup first.[/]");
            return;
        }

        var dataDir = AnsiConsole.Ask<string>("Data directory on host (press Enter for default):", "./bitcoin-data");
        
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

        var portMappings = new Dictionary<int, int>
        {
            { settings.BitcoinNodePort, 18332 },
            { 18333, 18333 }  // P2P port
        };

        await docker.StartContainerAsync(
            settings.BitcoinContainerName,
            "bitcoin-pocx",
            volumeMounts: volumeMounts,
            portMappings: portMappings,
            command: "bitcoind -printtoconsole -rpcport=18332 -rpcallowip=127.0.0.1 -rpcbind=0.0.0.0"
        );

        // Register with background service manager
        BackgroundServiceManager.RegisterService(
            settings.BitcoinContainerName,
            "Bitcoin-PoCX Node (Docker)"
        );
    }

    /// <summary>
    /// Stop Bitcoin-PoCX node container
    /// </summary>
    public static async Task StopBitcoinNodeContainerAsync(AppSettings settings)
    {
        var docker = GetDockerManager(settings);
        
        await docker.StopContainerAsync(settings.BitcoinContainerName);
        
        BackgroundServiceManager.RemoveService(settings.BitcoinContainerName);
    }

    /// <summary>
    /// Start PoCX miner container
    /// </summary>
    public static async Task StartMinerContainerAsync(AppSettings settings)
    {
        var docker = GetDockerManager(settings);

        if (!await docker.IsDockerAvailableAsync())
        {
            AnsiConsole.MarkupLine("[red]Docker is not available. Please run Docker Setup first.[/]");
            return;
        }

        if (!File.Exists(settings.MinerConfigPath))
        {
            AnsiConsole.MarkupLine($"[red]Config file not found at: {settings.MinerConfigPath}[/]");
            AnsiConsole.MarkupLine("[dim]Create a config.yaml file first.[/]");
            return;
        }

        var plotDir = AnsiConsole.Ask<string>("Plot directory on host:", settings.PlotDirectory);
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

        await docker.StartContainerAsync(
            settings.MinerContainerName,
            "pocx",
            volumeMounts: volumeMounts,
            command: $"pocx_miner -c /config/{configFileName}"
        );

        // Register with background service manager
        BackgroundServiceManager.RegisterService(
            settings.MinerContainerName,
            "PoCX Miner (Docker)"
        );
    }

    /// <summary>
    /// Stop PoCX miner container
    /// </summary>
    public static async Task StopMinerContainerAsync(AppSettings settings)
    {
        var docker = GetDockerManager(settings);
        
        await docker.StopContainerAsync(settings.MinerContainerName);
        
        BackgroundServiceManager.RemoveService(settings.MinerContainerName);
    }

    /// <summary>
    /// Start PoCX plotter container for creating plots
    /// </summary>
    public static async Task StartPlotterContainerAsync(AppSettings settings)
    {
        var docker = GetDockerManager(settings);

        if (!await docker.IsDockerAvailableAsync())
        {
            AnsiConsole.MarkupLine("[red]Docker is not available. Please run Docker Setup first.[/]");
            return;
        }

        var accountId = AnsiConsole.Ask<string>("Enter your [green]account ID[/]:");
        var plotDir = AnsiConsole.Ask<string>("Plot directory on host:", settings.PlotDirectory);
        var numWarps = AnsiConsole.Ask<int>("Number of warps (1 warp ≈ 1GB):", 10);

        // Create plot directory if it doesn't exist
        if (!Directory.Exists(plotDir))
        {
            Directory.CreateDirectory(plotDir);
        }

        var absolutePlotDir = Path.GetFullPath(plotDir);

        var volumeMounts = new Dictionary<string, string>
        {
            { absolutePlotDir, "/plots" }
        };

        AnsiConsole.MarkupLine("[yellow]Note: Plotter will run in background. Check logs with 'View Container Logs'[/]");

        await docker.StartContainerAsync(
            settings.PlotterContainerName,
            "pocx",
            volumeMounts: volumeMounts,
            command: $"pocx_plotter -a {accountId} -d /plots -w {numWarps}"
        );

        // Register with background service manager
        BackgroundServiceManager.RegisterService(
            settings.PlotterContainerName,
            "PoCX Plotter (Docker)"
        );
    }

    /// <summary>
    /// View logs from a container
    /// </summary>
    public static async Task ViewContainerLogsAsync(AppSettings settings)
    {
        var docker = GetDockerManager(settings);

        var containers = await docker.ListContainersAsync();
        if (!containers.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No containers found[/]");
            return;
        }

        var containerName = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select container to view logs:")
                .AddChoices(containers.Select(c => c.Name))
        );

        var tailLines = AnsiConsole.Ask<int>("Number of lines to show:", 50);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold]Logs for {containerName}:[/]");
        AnsiConsole.WriteLine();

        var logs = await docker.GetContainerLogsAsync(containerName, tailLines);
        
        var panel = new Panel(logs)
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Green)
        };
        
        AnsiConsole.Write(panel);
    }

    /// <summary>
    /// Start Electrs-PoCX server container (Electrum server)
    /// </summary>
    public static async Task StartElectrsContainerAsync(AppSettings settings)
    {
        var docker = GetDockerManager(settings);

        if (!await docker.IsDockerAvailableAsync())
        {
            AnsiConsole.MarkupLine("[red]Docker is not available. Please run Docker Setup first.[/]");
            return;
        }

        var dataDir = AnsiConsole.Ask<string>("Electrs data directory on host (press Enter for default):", "./electrs-data");
        
        // Create data directory if it doesn't exist
        if (!Directory.Exists(dataDir))
        {
            Directory.CreateDirectory(dataDir);
        }

        var absoluteDataDir = Path.GetFullPath(dataDir);

        // Ask for Bitcoin node connection details
        var daemonDir = AnsiConsole.Ask<string>("Bitcoin-PoCX data directory (press Enter for default):", "./bitcoin-data");
        var absoluteDaemonDir = Path.GetFullPath(daemonDir);

        var volumeMounts = new Dictionary<string, string>
        {
            { absoluteDataDir, "/data" },
            { absoluteDaemonDir, "/root/.bitcoin" }
        };

        var portMappings = new Dictionary<int, int>
        {
            { 3000, 3000 },  // HTTP API port
            { 50001, 50001 }  // Electrum RPC port
        };

        await docker.StartContainerAsync(
            settings.ElectrsContainerName,
            "electrs-pocx",
            volumeMounts: volumeMounts,
            portMappings: portMappings,
            command: "electrs --http-addr 0.0.0.0:3000 --electrum-rpc-addr 0.0.0.0:50001 --daemon-dir /root/.bitcoin --db-dir /data"
        );

        // Register with background service manager
        BackgroundServiceManager.RegisterService(
            settings.ElectrsContainerName,
            "Electrs-PoCX Server (Docker)"
        );
    }

    /// <summary>
    /// Stop Electrs-PoCX server container
    /// </summary>
    public static async Task StopElectrsContainerAsync(AppSettings settings)
    {
        var docker = GetDockerManager(settings);
        
        await docker.StopContainerAsync(settings.ElectrsContainerName);
        
        BackgroundServiceManager.RemoveService(settings.ElectrsContainerName);
    }

    /// <summary>
    /// Remove all PoCX containers
    /// </summary>
    public static async Task RemoveAllContainersAsync(AppSettings settings)
    {
        var docker = GetDockerManager(settings);

        if (!AnsiConsole.Confirm("Remove all PoCX containers? This will stop and delete them.", false))
        {
            return;
        }

        var containers = await docker.ListContainersAsync();
        foreach (var container in containers)
        {
            await docker.RemoveContainerAsync(container.Name);
        }

        AnsiConsole.MarkupLine("[green]✓[/] All containers removed");
    }
}

using PocxWallet.Protocol.Wrappers;
using PocxWallet.Cli.Configuration;
using PocxWallet.Cli.Services;
using Spectre.Console;

namespace PocxWallet.Cli.Commands;

/// <summary>
/// Plotting-related commands
/// </summary>
public static class PlottingCommands
{
    private static DockerServiceManager? _dockerManager;

    private static DockerServiceManager GetDockerManager()
    {
        if (_dockerManager == null)
        {
            _dockerManager = new DockerServiceManager();
        }
        return _dockerManager;
    }

    public static async Task CreatePlotAsync(AppSettings settings)
    {
        await CreatePlotDockerAsync(settings);
    }

    private static async Task CreatePlotDockerAsync(AppSettings settings)
    {
        var docker = GetDockerManager();

        if (!await docker.IsDockerAvailableAsync())
        {
            AnsiConsole.MarkupLine("[red]Docker is not available.[/]");
            AnsiConsole.MarkupLine("[dim]Install Docker using the Docker setup menu option[/]");
            return;
        }

        AnsiConsole.MarkupLine("[bold green]Create new plot file[/]");

        var accountId = AnsiConsole.Ask<string>("Enter [green]account ID[/]:");
        var plotPath = AnsiConsole.Ask<string>("Enter [green]plot directory[/]:", settings.PlotDirectory);
        var warps = AnsiConsole.Ask<int>("Enter number of [green]warps[/] (1 warp ~= 1GB):", settings.Plotter.DefaultWarps);

        // Create plot directory if it doesn't exist
        if (!Directory.Exists(plotPath))
        {
            Directory.CreateDirectory(plotPath);
        }

        var absolutePlotDir = Path.GetFullPath(plotPath);

        var volumeMounts = new Dictionary<string, string>
        {
            { absolutePlotDir, "/plots" }
        };

        // Build environment variables from settings
        var envVars = new Dictionary<string, string>(settings.Plotter.EnvironmentVariables);

        AnsiConsole.MarkupLine("[yellow]Note: Plotter will run in background. Use 'View Logs' to monitor progress.[/]");
        AnsiConsole.WriteLine();

        var command = $"pocx_plotter -a {accountId} -d /plots -w {warps} {settings.Plotter.AdditionalParams}";

        var success = await docker.StartContainerAsync(
            settings.PlotterContainerName,
            "pocx",
            settings.Plotter.Repository,
            settings.Plotter.Tag,
            environmentVars: envVars.Count > 0 ? envVars : null,
            volumeMounts: volumeMounts,
            command: command
        );

        if (success)
        {
            // Register with background service manager
            BackgroundServiceManager.RegisterService(
                settings.PlotterContainerName,
                "PoCX Plotter (Docker)"
            );
        }
    }

    public static async Task ViewLogsAsync(AppSettings settings)
    {
        var docker = GetDockerManager();
        var lines = AnsiConsole.Ask("How many log lines to display?", 50);
        await docker.DisplayContainerLogsAsync(settings.PlotterContainerName, lines, "Plotter Logs");
    }
}

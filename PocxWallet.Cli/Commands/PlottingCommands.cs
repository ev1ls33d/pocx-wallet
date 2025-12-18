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

    private static DockerServiceManager GetDockerManager(AppSettings settings)
    {
        if (_dockerManager == null)
        {
            _dockerManager = new DockerServiceManager(settings.DockerRegistry, settings.DockerImageTag);
        }
        return _dockerManager;
    }

    public static async Task CreatePlotAsync(AppSettings settings)
    {
        if (settings.UseDocker)
        {
            await CreatePlotDockerAsync(settings);
        }
        else
        {
            await CreatePlotNativeAsync(settings.PoCXBinariesPath);
        }
    }

    private static async Task CreatePlotDockerAsync(AppSettings settings)
    {
        var docker = GetDockerManager(settings);

        if (!await docker.IsDockerAvailableAsync())
        {
            AnsiConsole.MarkupLine("[red]Docker is not available.[/]");
            AnsiConsole.MarkupLine("[dim]Install Docker or disable Docker mode in Settings[/]");
            return;
        }

        AnsiConsole.MarkupLine("[bold green]Create new plot file[/]");

        var accountId = AnsiConsole.Ask<string>("Enter [green]account ID[/]:");
        var plotPath = AnsiConsole.Ask<string>("Enter [green]plot directory[/]:", settings.PlotDirectory);
        var warps = AnsiConsole.Ask<int>("Enter number of [green]warps[/] (1 warp ~= 1GB):", 10);

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

        AnsiConsole.MarkupLine("[yellow]Note: Plotter will run in background. Use 'Docker Management → View Logs' to monitor progress.[/]");
        AnsiConsole.WriteLine();

        var success = await docker.StartContainerAsync(
            settings.PlotterContainerName,
            "pocx",
            volumeMounts: volumeMounts,
            command: $"pocx_plotter -a {accountId} -d /plots -w {warps}"
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

    private static async Task CreatePlotNativeAsync(string binariesPath)
    {
        AnsiConsole.MarkupLine("[bold green]Create new plot file[/]");

        var accountId = AnsiConsole.Ask<string>("Enter [green]account ID[/]:");
        var plotPath = AnsiConsole.Ask<string>("Enter [green]plot directory[/]:", "./plots");
        var warps = AnsiConsole.Ask<int>("Enter number of [green]warps[/] (1 warp ~= 1GB):", 10);

        var plotterPath = Path.Combine(binariesPath, "pocx_plotter");
        if (!File.Exists(plotterPath))
        {
            AnsiConsole.MarkupLine($"[red]Plotter binary not found at: {plotterPath}[/]");
            return;
        }

        using var plotter = new PlotterWrapper(plotterPath);

        try
        {
            await AnsiConsole.Progress()
                .AutoRefresh(true)
                .Columns(
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn())
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("[green]Plotting...[/]");
                    task.IsIndeterminate = true;

                    var progress = new Progress<string>(output =>
                    {
                        AnsiConsole.MarkupLine($"[dim]{output}[/]");
                    });

                    var result = await plotter.CreatePlotAsync(
                        accountId,
                        plotPath,
                        warps,
                        progress);

                    task.StopTask();

                    if (result.IsSuccess)
                    {
                        AnsiConsole.MarkupLine("[green]√[/] Plot created successfully!");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]‼[/] Plotting failed: {result.Error}");
                    }
                });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }


}

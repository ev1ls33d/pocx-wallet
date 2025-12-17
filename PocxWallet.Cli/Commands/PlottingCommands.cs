using PocxWallet.Protocol.Wrappers;
using Spectre.Console;

namespace PocxWallet.Cli.Commands;

/// <summary>
/// Plotting-related commands
/// </summary>
public static class PlottingCommands
{
    public static async Task CreatePlotAsync(string binariesPath)
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

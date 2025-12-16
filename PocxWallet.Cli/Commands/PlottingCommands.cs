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
        var warps = AnsiConsole.Ask<int>("Enter number of [green]warps[/] (1 warp ≈ 1GB):", 10);

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
                        AnsiConsole.MarkupLine("[green]✓[/] Plot created successfully!");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]✗ Plotting failed:[/] {result.Error}");
                    }
                });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }

    public static async Task VerifyPlotAsync(string binariesPath)
    {
        AnsiConsole.MarkupLine("[bold green]Verify plot file[/]");

        var plotFile = AnsiConsole.Ask<string>("Enter [green]plot file path[/]:");

        if (!File.Exists(plotFile))
        {
            AnsiConsole.MarkupLine("[red]Plot file not found![/]");
            return;
        }

        var verifierPath = Path.Combine(binariesPath, "pocx_verifier");
        if (!File.Exists(verifierPath))
        {
            AnsiConsole.MarkupLine($"[red]Verifier binary not found at: {verifierPath}[/]");
            return;
        }

        using var verifier = new VerifierWrapper(verifierPath);

        try
        {
            await AnsiConsole.Status()
                .StartAsync("Verifying plot...", async ctx =>
                {
                    var progress = new Progress<string>(output =>
                    {
                        AnsiConsole.MarkupLine($"[dim]{output}[/]");
                    });

                    var result = await verifier.VerifyPlotAsync(plotFile, progress);

                    if (result.IsSuccess)
                    {
                        AnsiConsole.MarkupLine("[green]✓[/] Plot verification successful!");
                        AnsiConsole.MarkupLine($"[dim]{result.Output}[/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]✗ Verification failed:[/] {result.Error}");
                    }
                });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }
}

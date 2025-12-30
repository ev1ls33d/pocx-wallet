using PocxWallet.Core.Services;
using Spectre.Console;

namespace PocxWallet.Cli.Services;

/// <summary>
/// CLI-specific background service display using Spectre.Console
/// </summary>
public static class CliBackgroundServiceDisplay
{
    public static void DisplayServices()
    {
        var services = BackgroundServiceManager.GetAllServices().ToList();
        
        if (!services.Any())
        {
            AnsiConsole.MarkupLine("[dim]No background services running[/]");
            return;
        }

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("Service");
        table.AddColumn("Status");
        table.AddColumn("Runtime");

        foreach (var (id, info) in services)
        {
            var runtime = info.StartTime.HasValue 
                ? (DateTime.Now - info.StartTime.Value).ToString(@"hh\:mm\:ss")
                : "N/A";

            var statusColor = info.Status == "Running" ? "green" : 
                             info.Status == "Stopped" ? "red" : "yellow";

            table.AddRow(
                info.Name,
                $"[{statusColor}]{info.Status}[/]",
                runtime
            );
        }

        AnsiConsole.Write(table);
    }
}

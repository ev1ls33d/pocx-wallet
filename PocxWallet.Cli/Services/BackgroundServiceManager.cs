using Spectre.Console;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace PocxWallet.Cli.Services;

/// <summary>
/// Manages background services like plotting, mining, and the bitcoin node
/// </summary>
public class BackgroundServiceManager
{
    private static readonly ConcurrentDictionary<string, ServiceInfo> _services = new();

    public class ServiceInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = "Stopped";
        public DateTime? StartTime { get; set; }
        public Process? Process { get; set; }
        public CancellationTokenSource? CancellationToken { get; set; }
    }

    public static void RegisterService(string id, string name, Process? process = null)
    {
        _services[id] = new ServiceInfo
        {
            Name = name,
            Status = "Running",
            StartTime = DateTime.Now,
            Process = process
        };
    }

    public static void UpdateServiceStatus(string id, string status)
    {
        if (_services.TryGetValue(id, out var service))
        {
            service.Status = status;
        }
    }

    public static void RemoveService(string id)
    {
        _services.TryRemove(id, out _);
    }

    public static void DisplayServices()
    {
        if (_services.IsEmpty)
        {
            AnsiConsole.MarkupLine("[dim]No background services running[/]");
            return;
        }

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("Service");
        table.AddColumn("Status");
        table.AddColumn("Runtime");

        foreach (var kvp in _services)
        {
            var runtime = kvp.Value.StartTime.HasValue 
                ? (DateTime.Now - kvp.Value.StartTime.Value).ToString(@"hh\:mm\:ss")
                : "N/A";

            var statusColor = kvp.Value.Status == "Running" ? "green" : 
                             kvp.Value.Status == "Stopped" ? "red" : "yellow";

            table.AddRow(
                kvp.Value.Name,
                $"[{statusColor}]{kvp.Value.Status}[/]",
                runtime
            );
        }

        AnsiConsole.Write(table);
    }

    public static void StopService(string id)
    {
        if (_services.TryGetValue(id, out var service))
        {
            try
            {
                service.Process?.Kill(true);
                service.CancellationToken?.Cancel();
            }
            catch { }
            service.Status = "Stopped";
        }
    }

    public static void StopAllServices()
    {
        foreach (var kvp in _services)
        {
            StopService(kvp.Key);
        }
    }

    public static IEnumerable<(string Id, ServiceInfo Info)> GetAllServices()
    {
        return _services.Select(kvp => (kvp.Key, kvp.Value));
    }

    public static bool HasRunningServices()
    {
        return _services.Any(s => s.Value.Status == "Running");
    }
}

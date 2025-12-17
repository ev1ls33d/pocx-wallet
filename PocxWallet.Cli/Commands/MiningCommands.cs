using PocxWallet.Protocol.Wrappers;
using PocxWallet.Cli.Services;
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

    public static void StartMining(string binariesPath, string configPath)
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

            AnsiConsole.MarkupLine("[green][OK][/] Miner started as background service!");
            AnsiConsole.MarkupLine("[dim]Check 'Background Services' section in main menu[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }

    public static void StopMining()
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

        AnsiConsole.MarkupLine("[green][OK][/] Miner stopped");
    }

    public static void ShowMiningStatus()
    {
        if (_activeMiner?.IsRunning == true)
        {
            AnsiConsole.MarkupLine("[green][OK][/] Miner is running");
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
        AnsiConsole.MarkupLine($"[green][OK][/] Configuration saved to: {configPath}");
    }
}

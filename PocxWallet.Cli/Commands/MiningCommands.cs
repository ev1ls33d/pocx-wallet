using PocxWallet.Protocol.Wrappers;
using Spectre.Console;

namespace PocxWallet.Cli.Commands;

/// <summary>
/// Mining-related commands
/// </summary>
public static class MiningCommands
{
    private static MinerWrapper? _activeMiner;

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

            AnsiConsole.MarkupLine("[bold green]Starting miner...[/]");
            AnsiConsole.MarkupLine($"[dim]Config: {configPath}[/]");
            AnsiConsole.WriteLine();

            _activeMiner.StartMining(
                configPath,
                onOutput: output => AnsiConsole.MarkupLine($"[green]MINER:[/] {output}"),
                onError: error => AnsiConsole.MarkupLine($"[red]ERROR:[/] {error}"));

            AnsiConsole.MarkupLine("[green]✓[/] Miner started successfully!");
            AnsiConsole.MarkupLine("[dim]Press Ctrl+C to stop mining[/]");
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
            });

        AnsiConsole.MarkupLine("[green]✓[/] Miner stopped");
    }

    public static void ShowMiningStatus()
    {
        if (_activeMiner?.IsRunning == true)
        {
            AnsiConsole.MarkupLine("[green]✓ Miner is running[/]");
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
        AnsiConsole.MarkupLine($"[green]✓[/] Configuration saved to: {configPath}");
    }
}

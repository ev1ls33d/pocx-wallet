using PocxWallet.Protocol.Wrappers;
using PocxWallet.Cli.Services;
using Spectre.Console;

namespace PocxWallet.Cli.Commands;

/// <summary>
/// Bitcoin-PoCX node commands
/// </summary>
public static class NodeCommands
{
    private static BitcoinNodeWrapper? _activeNode;
    private static BitcoinCliWrapper? _cliWrapper;
    private const string SERVICE_ID = "bitcoin-node";

    public static void StartNode(string binariesPath, string? dataDir = null, int rpcPort = 18883)
    {
        if (_activeNode?.IsRunning == true)
        {
            AnsiConsole.MarkupLine("[yellow]Bitcoin-PoCX node is already running![/]");
            return;
        }

        var nodePath = Path.Combine(binariesPath, "bitcoind");
        if (!File.Exists(nodePath))
        {
            AnsiConsole.MarkupLine($"[red]bitcoind binary not found at: {nodePath}[/]");
            AnsiConsole.MarkupLine("[dim]Make sure Bitcoin-PoCX is built in the bitcoin-pocx directory[/]");
            return;
        }

        try
        {
            _activeNode = new BitcoinNodeWrapper(nodePath);

            AnsiConsole.MarkupLine("[bold green]Starting Bitcoin-PoCX node as background service...[/]");
            AnsiConsole.MarkupLine($"[dim]RPC Port: {rpcPort}[/]");
            if (!string.IsNullOrEmpty(dataDir))
            {
                AnsiConsole.MarkupLine($"[dim]Data Directory: {dataDir}[/]");
            }
            AnsiConsole.WriteLine();

            _activeNode.StartNode(
                dataDir,
                rpcPort,
                onOutput: output => { }, // Silent in background
                onError: error => { });

            // Initialize CLI wrapper
            var cliPath = Path.Combine(binariesPath, "bitcoin-cli");
            if (File.Exists(cliPath))
            {
                _cliWrapper = new BitcoinCliWrapper(cliPath, rpcPort);
            }

            // Register as background service
            BackgroundServiceManager.RegisterService(SERVICE_ID, "Bitcoin-PoCX Node");

            AnsiConsole.MarkupLine("[green]√[/] Bitcoin-PoCX node started as background service!");
            AnsiConsole.MarkupLine("[dim]Check 'Background Services' section in main menu[/]");
            AnsiConsole.MarkupLine($"[dim]RPC available at localhost:{rpcPort}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }

    public static void StopNode()
    {
        if (_activeNode?.IsRunning != true)
        {
            AnsiConsole.MarkupLine("[yellow]No active node to stop[/]");
            return;
        }

        AnsiConsole.Status()
            .Start("Stopping Bitcoin-PoCX node...", ctx =>
            {
                try
                {
                    // Try graceful shutdown via CLI if available
                    if (_cliWrapper != null)
                    {
                        var stopTask = _cliWrapper.StopNodeAsync();
                        stopTask.Wait(TimeSpan.FromSeconds(5));
                    }
                }
                catch { }

                _activeNode.StopProcess();
                _activeNode.Dispose();
                _activeNode = null;
                _cliWrapper = null;

                // Remove from background services
                BackgroundServiceManager.RemoveService(SERVICE_ID);
            });

        AnsiConsole.MarkupLine("[green]√[/] Bitcoin-PoCX node stopped");
    }

    public static void ShowNodeStatus()
    {
        if (_activeNode?.IsRunning != true)
        {
            AnsiConsole.MarkupLine("[dim]Bitcoin-PoCX node is not running[/]");
            return;
        }

        AnsiConsole.MarkupLine("[green]√[/] Bitcoin-PoCX node is running");

        if (_cliWrapper != null)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Status()
                .Start("Fetching node info...", ctx =>
                {
                    try
                    {
                        var infoTask = _cliWrapper.GetBlockchainInfoAsync();
                        infoTask.Wait(TimeSpan.FromSeconds(5));
                        
                        if (infoTask.IsCompletedSuccessfully)
                        {
                            AnsiConsole.WriteLine();
                            AnsiConsole.MarkupLine("[bold]Blockchain Info:[/]");
                            AnsiConsole.WriteLine(infoTask.Result);
                        }
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[yellow]Could not fetch info: {ex.Message}[/]");
                        AnsiConsole.MarkupLine("[dim]Node may still be starting up...[/]");
                    }
                });
        }
    }

    public static async Task CheckAddressBalanceAsync(string address)
    {
        if (_cliWrapper == null)
        {
            AnsiConsole.MarkupLine("[yellow]Bitcoin-PoCX node is not running[/]");
            AnsiConsole.MarkupLine("[dim]Start the node first from the Node menu[/]");
            return;
        }

        try
        {
            AnsiConsole.MarkupLine($"[bold]Checking balance for:[/] [green]{address}[/]");
            
            await AnsiConsole.Status()
                .StartAsync("Querying node...", async ctx =>
                {
                    var balance = await _cliWrapper.GetBalanceAsync();
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine($"[bold]Balance:[/] {balance}");
                });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }

    public static async Task SendTransactionAsync(string toAddress, decimal amount)
    {
        if (_cliWrapper == null)
        {
            AnsiConsole.MarkupLine("[yellow]Bitcoin-PoCX node is not running[/]");
            AnsiConsole.MarkupLine("[dim]Start the node first from the Node menu[/]");
            return;
        }

        try
        {
            if (!AnsiConsole.Confirm($"Send {amount} PoCX to {toAddress}?", false))
            {
                AnsiConsole.MarkupLine("[yellow]Transaction cancelled[/]");
                return;
            }

            await AnsiConsole.Status()
                .StartAsync("Broadcasting transaction...", async ctx =>
                {
                    var txid = await _cliWrapper.SendToAddressAsync(toAddress, amount);
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("[green]√[/] Transaction sent!");
                    AnsiConsole.MarkupLine($"[bold]Transaction ID:[/] {txid}");
                });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }

    public static BitcoinCliWrapper? GetCliWrapper()
    {
        return _cliWrapper;
    }

    public static bool IsNodeRunning()
    {
        return _activeNode?.IsRunning == true;
    }
}

using PocxWallet.Core.Wallet;
using PocxWallet.Cli.Configuration;
using Spectre.Console;

namespace PocxWallet.Cli.Commands;

/// <summary>
/// Transaction-related commands for sending funds and checking balance
/// </summary>
public static class TransactionCommands
{
    public static async Task CheckBalance()
    {
        AnsiConsole.MarkupLine("[bold green]Check Balance[/]");
        
        var walletName = AnsiConsole.Ask<string>("Enter wallet name:", "pocx_wallet");
        
        // Load settings
        var settings = LoadSettings();

        try
        {
            // Ensure wallet is loaded
            var walletLoaded = await NodeCommands.EnsureWalletLoadedAsync(settings, walletName);
            if (!walletLoaded)
            {
                AnsiConsole.MarkupLine($"[red]Wallet '{walletName}' not found in Bitcoin node[/]");
                AnsiConsole.MarkupLine("[dim]Create or import a wallet first[/]");
                return;
            }

            AnsiConsole.WriteLine();
            await AnsiConsole.Status()
                .StartAsync("Fetching balance...", async ctx =>
                {
                    var balance = await NodeCommands.ExecuteBitcoinCliDockerAsync(settings, "getbalance");

                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine($"[bold]Wallet Balance:[/] [green]{balance}[/]");
                });

            // Also show wallet info
            AnsiConsole.WriteLine();
            var walletInfo = await NodeCommands.ExecuteBitcoinCliDockerAsync(settings, "getwalletinfo");

            var panel = new Panel(walletInfo)
            {
                Header = new PanelHeader("[bold]Wallet Info[/]"),
                Border = BoxBorder.Rounded
            };
            AnsiConsole.Write(panel);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }

    public static async Task SendFunds()
    {
        AnsiConsole.MarkupLine("[bold green]Send Funds[/]");
        
        var walletName = AnsiConsole.Ask<string>("Enter wallet name:", "pocx_wallet");
        
        // Load settings
        var settings = LoadSettings();

        try
        {
            // Ensure wallet is loaded
            var walletLoaded = await NodeCommands.EnsureWalletLoadedAsync(settings, walletName);
            if (!walletLoaded)
            {
                AnsiConsole.MarkupLine($"[red]Wallet '{walletName}' not found in Bitcoin node[/]");
                AnsiConsole.MarkupLine("[dim]Create or import a wallet first[/]");
                return;
            }

            AnsiConsole.WriteLine();
            var toAddress = AnsiConsole.Ask<string>("Enter recipient [green]address[/]:");
            var amount = AnsiConsole.Ask<decimal>("Enter [green]amount[/] to send:");

            if (!AnsiConsole.Confirm($"Send {amount} PoCX to {toAddress}?", false))
            {
                AnsiConsole.MarkupLine("[yellow]Transaction cancelled[/]");
                return;
            }

            await AnsiConsole.Status()
                .StartAsync("Broadcasting transaction...", async ctx =>
                {
                    var txid = await NodeCommands.ExecuteBitcoinCliDockerAsync(settings, "sendtoaddress", toAddress, amount.ToString());

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

    public static async Task ShowTransactionHistory()
    {
        AnsiConsole.MarkupLine("[bold green]Transaction History[/]");
        
        var walletName = AnsiConsole.Ask<string>("Enter wallet name:", "pocx_wallet");
        var count = AnsiConsole.Ask<int>("How many recent transactions to display?", 10);
        
        // Load settings
        var settings = LoadSettings();

        try
        {
            // Ensure wallet is loaded
            var walletLoaded = await NodeCommands.EnsureWalletLoadedAsync(settings, walletName);
            if (!walletLoaded)
            {
                AnsiConsole.MarkupLine($"[red]Wallet '{walletName}' not found in Bitcoin node[/]");
                AnsiConsole.MarkupLine("[dim]Create or import a wallet first[/]");
                return;
            }

            AnsiConsole.WriteLine();
            await AnsiConsole.Status()
                .StartAsync("Fetching transactions...", async ctx =>
                {
                    var txs = await NodeCommands.ExecuteBitcoinCliDockerAsync(settings, "listtransactions", "*", count.ToString());

                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine($"[bold]Recent Transactions (last {count}):[/]");
                    
                    var panel = new Panel(txs)
                    {
                        Border = BoxBorder.Rounded,
                        BorderStyle = new Style(Color.Green)
                    };
                    AnsiConsole.Write(panel);
                });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }

    private static AppSettings LoadSettings()
    {
        return SettingsManager.LoadSettings();
    }
}

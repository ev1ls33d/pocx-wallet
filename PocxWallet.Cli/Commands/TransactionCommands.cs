using PocxWallet.Core.Wallet;
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
        
        var walletPath = AnsiConsole.Ask<string>("Enter wallet file path:", "./wallet.json");

        if (!File.Exists(walletPath))
        {
            AnsiConsole.MarkupLine("[red]Wallet file not found![/]");
            return;
        }

        try
        {
            var json = File.ReadAllText(walletPath);
            var walletData = System.Text.Json.JsonDocument.Parse(json);
            var mnemonic = walletData.RootElement.GetProperty("mnemonic").GetString();

            if (string.IsNullOrEmpty(mnemonic))
            {
                AnsiConsole.MarkupLine("[red]Invalid wallet file![/]");
                return;
            }

            var wallet = HDWallet.FromMnemonic(mnemonic);
            var address = wallet.GetPoCXAddress(0, 0);

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[bold]Address:[/] [green]{address}[/]");
            AnsiConsole.WriteLine();

            // Check if node is running
            if (NodeCommands.IsNodeRunning())
            {
                await NodeCommands.CheckAddressBalanceAsync(address);
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]Note:[/] Balance checking requires a running Bitcoin-PoCX node.");
                AnsiConsole.MarkupLine("[dim]Start the node from [Node] Bitcoin-PoCX Node menu[/]");
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }

    public static async Task SendFunds()
    {
        AnsiConsole.MarkupLine("[bold green]Send Funds[/]");
        
        var walletPath = AnsiConsole.Ask<string>("Enter wallet file path:", "./wallet.json");

        if (!File.Exists(walletPath))
        {
            AnsiConsole.MarkupLine("[red]Wallet file not found![/]");
            return;
        }

        try
        {
            var json = File.ReadAllText(walletPath);
            var walletData = System.Text.Json.JsonDocument.Parse(json);
            var mnemonic = walletData.RootElement.GetProperty("mnemonic").GetString();

            if (string.IsNullOrEmpty(mnemonic))
            {
                AnsiConsole.MarkupLine("[red]Invalid wallet file![/]");
                return;
            }

            var wallet = HDWallet.FromMnemonic(mnemonic);
            var fromAddress = wallet.GetPoCXAddress(0, 0);

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[bold]From Address:[/] [green]{fromAddress}[/]");
            AnsiConsole.WriteLine();

            var toAddress = AnsiConsole.Ask<string>("Enter recipient [green]address[/]:");
            var amount = AnsiConsole.Ask<decimal>("Enter [green]amount[/] to send:");

            // Check if node is running
            if (NodeCommands.IsNodeRunning())
            {
                await NodeCommands.SendTransactionAsync(toAddress, amount);
            }
            else
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[yellow]Note:[/] Transaction broadcasting requires a running Bitcoin-PoCX node.");
                AnsiConsole.MarkupLine("[dim]Start the node from [Node] Bitcoin-PoCX Node menu[/]");
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }

    public static async Task ShowTransactionHistory()
    {
        AnsiConsole.MarkupLine("[bold green]Transaction History[/]");
        
        var walletPath = AnsiConsole.Ask<string>("Enter wallet file path:", "./wallet.json");

        if (!File.Exists(walletPath))
        {
            AnsiConsole.MarkupLine("[red]Wallet file not found![/]");
            return;
        }

        try
        {
            var json = File.ReadAllText(walletPath);
            var walletData = System.Text.Json.JsonDocument.Parse(json);
            var mnemonic = walletData.RootElement.GetProperty("mnemonic").GetString();

            if (string.IsNullOrEmpty(mnemonic))
            {
                AnsiConsole.MarkupLine("[red]Invalid wallet file![/]");
                return;
            }

            var wallet = HDWallet.FromMnemonic(mnemonic);
            var address = wallet.GetPoCXAddress(0, 0);

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[bold]Address:[/] [green]{address}[/]");
            AnsiConsole.WriteLine();

            // Check if node is running
            if (NodeCommands.IsNodeRunning())
            {
                var cli = NodeCommands.GetCliWrapper();
                if (cli != null)
                {
                    try
                    {
                        await AnsiConsole.Status()
                            .StartAsync("Fetching transactions...", async ctx =>
                            {
                                var txs = await cli.ListTransactionsAsync(10);
                                AnsiConsole.WriteLine();
                                AnsiConsole.MarkupLine("[bold]Recent Transactions:[/]");
                                AnsiConsole.WriteLine(txs);
                            });
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
                    }
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]Note:[/] Transaction history requires a running Bitcoin-PoCX node.");
                AnsiConsole.MarkupLine("[dim]Start the node from [Node] Bitcoin-PoCX Node menu[/]");
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }
}

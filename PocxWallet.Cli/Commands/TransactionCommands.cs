using PocxWallet.Core.Wallet;
using Spectre.Console;

namespace PocxWallet.Cli.Commands;

/// <summary>
/// Transaction-related commands for sending funds and checking balance
/// </summary>
public static class TransactionCommands
{
    public static void CheckBalance()
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

            // Note: Actual balance checking requires connection to a node
            AnsiConsole.MarkupLine("[yellow]Note:[/] Balance checking requires a connection to a Bitcoin-PoCX node.");
            AnsiConsole.MarkupLine("[dim]Feature implementation in progress - node connection needed[/]");
            
            // Placeholder for future implementation
            AnsiConsole.MarkupLine($"[dim]Balance: [To be implemented - requires node RPC connection][/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }

    public static void SendFunds()
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

            if (!AnsiConsole.Confirm($"Send {amount} PoCX to {toAddress}?", false))
            {
                AnsiConsole.MarkupLine("[yellow]Transaction cancelled[/]");
                return;
            }

            // Note: Actual transaction sending requires connection to a node
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[yellow]Note:[/] Transaction broadcasting requires a connection to a Bitcoin-PoCX node.");
            AnsiConsole.MarkupLine("[dim]Feature implementation in progress - node RPC connection needed[/]");
            
            // Placeholder for future implementation
            AnsiConsole.MarkupLine($"[dim]Would send: {amount} PoCX from {fromAddress} to {toAddress}[/]");
            AnsiConsole.MarkupLine($"[dim]Transaction signing and broadcasting requires node connection[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }

    public static void ShowTransactionHistory()
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

            // Note: Transaction history requires connection to a node
            AnsiConsole.MarkupLine("[yellow]Note:[/] Transaction history requires a connection to a Bitcoin-PoCX node.");
            AnsiConsole.MarkupLine("[dim]Feature implementation in progress - node RPC connection needed[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }
}

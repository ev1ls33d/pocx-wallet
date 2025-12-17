using PocxWallet.Core.Wallet;
using Spectre.Console;
using NBitcoin;

namespace PocxWallet.Cli.Commands;

/// <summary>
/// Wallet-related commands
/// </summary>
public static class WalletCommands
{
    public static void CreateNewWallet()
    {
        AnsiConsole.MarkupLine("[bold green]Creating new HD wallet...[/]");
        
        var wordCount = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select mnemonic [green]word count[/]:")
                .AddChoices("12 words", "15 words", "18 words", "21 words", "24 words"));

        var wordCountValue = wordCount switch
        {
            "12 words" => WordCount.Twelve,
            "15 words" => WordCount.Fifteen,
            "18 words" => WordCount.Eighteen,
            "21 words" => WordCount.TwentyOne,
            "24 words" => WordCount.TwentyFour,
            _ => WordCount.Twelve
        };

        var usePassphrase = AnsiConsole.Confirm("Use additional passphrase?", false);
        string? passphrase = null;

        if (usePassphrase)
        {
            passphrase = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter passphrase:")
                    .Secret());
        }

        var wallet = HDWallet.CreateNew(wordCountValue, passphrase);

        AnsiConsole.WriteLine();
        var panel = new Panel(new Markup($"[yellow]{wallet.MnemonicPhrase}[/]"))
        {
            Header = new PanelHeader("[bold]Mnemonic Phrase[/]"),
            Border = BoxBorder.Double
        };
        AnsiConsole.Write(panel);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold]Address:[/] [green]{wallet.GetPoCXAddress()}[/]");
        AnsiConsole.MarkupLine($"[dim]Format: Bech32 (pocx1q...)[/]");
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]WIF Mainnet:[/] [dim]{0}[/]", wallet.GetWIFMainnet());
        AnsiConsole.MarkupLine("[bold]WIF Testnet:[/] [dim]{0}[/]", wallet.GetWIFTestnet());
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Descriptor (Mainnet):[/] [dim]{0}[/]", wallet.GetDescriptorMainnet());
        AnsiConsole.MarkupLine("[bold]Descriptor (Testnet):[/] [dim]{0}[/]", wallet.GetDescriptorTestnet());

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold red]▲ IMPORTANT: Save your mnemonic phrase in a secure location![/]");
        AnsiConsole.MarkupLine("[dim]This is the only way to recover your wallet if you lose access.[/]");

        if (AnsiConsole.Confirm("Save wallet to file?", true))
        {
            var filePath = AnsiConsole.Ask<string>("Enter file path:", "./wallet.json");
            File.WriteAllText(filePath, wallet.ExportToJson());
            AnsiConsole.MarkupLine($"[green]√[/] Wallet saved to: {filePath}");
        }
    }

    public static void RestoreWallet()
    {
        AnsiConsole.MarkupLine("[bold green]Restore wallet from mnemonic[/]");
        
        var mnemonic = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter your [green]mnemonic phrase[/]:")
                .Validate(m =>
                {
                    try
                    {
                        new Mnemonic(m);
                        return ValidationResult.Success();
                    }
                    catch
                    {
                        return ValidationResult.Error("[red]Invalid mnemonic phrase[/]");
                    }
                }));

        var usePassphrase = AnsiConsole.Confirm("Did you use a passphrase?", false);
        string? passphrase = null;

        if (usePassphrase)
        {
            passphrase = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter passphrase:")
                    .Secret());
        }

        try
        {
            var wallet = HDWallet.FromMnemonic(mnemonic, passphrase);
            
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[green]√[/] Wallet restored successfully!");
            AnsiConsole.MarkupLine($"[bold]Address:[/] [green]{wallet.GetPoCXAddress()}[/]");
            AnsiConsole.MarkupLine($"[dim]Format: Bech32 (pocx1q...)[/]");
            
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]WIF Mainnet:[/] [dim]{0}[/]", wallet.GetWIFMainnet());
            AnsiConsole.MarkupLine("[bold]WIF Testnet:[/] [dim]{0}[/]", wallet.GetWIFTestnet());
            
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Descriptor (Mainnet):[/] [dim]{0}[/]", wallet.GetDescriptorMainnet());
            AnsiConsole.MarkupLine("[bold]Descriptor (Testnet):[/] [dim]{0}[/]", wallet.GetDescriptorTestnet());

            if (AnsiConsole.Confirm("Save wallet to file?", true))
            {
                var filePath = AnsiConsole.Ask<string>("Enter file path:", "./wallet.json");
                File.WriteAllText(filePath, wallet.ExportToJson());
                AnsiConsole.MarkupLine($"[green]√[/] Wallet saved to: {filePath}");
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }

    public static void ShowAddresses()
    {
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

            var count = AnsiConsole.Ask("How many addresses to display?", 5);

            var table = new Table();
            table.AddColumn("Index");
            table.AddColumn("Account ID");
            table.AddColumn("Public Key");

            for (uint i = 0; i < count; i++)
            {
                var address = wallet.GetPoCXAddress(0, i);
                var pubKey = wallet.GetPublicKey(0, i);
                table.AddRow(i.ToString(), address, pubKey[..16] + "...");
            }

            AnsiConsole.Write(table);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }
}

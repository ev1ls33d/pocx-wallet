using PocxWallet.Core.VanityAddress;
using PocxWallet.Core.Wallet;
using Spectre.Console;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PocxWallet.Cli.Commands;

/// <summary>
/// Vanity address generation commands
/// </summary>
public static class VanityCommands
{
    // Valid Bech32 characters (excluding '1', 'b', 'i', 'o' as per spec)
    private const string ValidBech32Chars = "qpzry9x8gf2tvdw0s3jn54khce6mua7l";
    private const string ValidBech32CharsSorted = "0234567890acdefghjklmnpqrstuvwxyz"; // sorted for display

    public static async Task GenerateVanityAddressAsync()
    {
        AnsiConsole.MarkupLine("[bold green]Generate vanity address[/]");
        AnsiConsole.MarkupLine("[dim]Note: This may take a long time depending on the pattern complexity[/]");
        
        // Sort and display valid characters
        var validCharsSorted = string.Concat(ValidBech32Chars.OrderBy(c => c));
        AnsiConsole.MarkupLine($"[dim]Valid characters: {validCharsSorted} (case-insensitive)[/]");
        AnsiConsole.WriteLine();

        string pattern;
        while (true)
        {
            pattern = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter [green]pattern[/] to search for (or '!' to exit):")
                    .AllowEmpty());

            if (pattern.Equals("!", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(pattern))
            {
                AnsiConsole.MarkupLine("[yellow]Operation cancelled[/]");
                return;
            }

            // Validate pattern contains only valid Bech32 characters
            if (!IsValidBech32Pattern(pattern))
            {
                AnsiConsole.MarkupLine("[red]Invalid pattern![/] Only these characters are allowed:");
                AnsiConsole.MarkupLine($"[yellow]{validCharsSorted}[/]");
                AnsiConsole.WriteLine();
                continue;
            }

            break;
        }

        var useGpu = AnsiConsole.Confirm("Use GPU acceleration?", false);

        if (useGpu)
        {
            AnsiConsole.MarkupLine("[yellow]Note: GPU acceleration is not yet fully implemented[/]");
        }

        var generator = new VanityAddressGenerator(pattern, useGpu);
        var cts = new CancellationTokenSource();

        try
        {
            // Verwende den konkreten, erwarteten Rückgabetyp (named ValueTuple).
            (string Mnemonic, string Address) result = default;

            // Run the generator inside the progress scope but do NOT run interactive prompts inside it.
            await AnsiConsole.Progress()
                .AutoRefresh(true)
                .AutoClear(true) // clear progress before subsequent interactive prompts
                .Columns(
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new SpinnerColumn())
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("[green]Searching for vanity address...[/]");
                    task.IsIndeterminate = true;

                    var progress = new Progress<long>(attempts =>
                    {
                        task.Description = $"[green]Searching... ({attempts:N0} attempts)[/]";
                    });

                    // Generator liefert tatsächlich (string Mnemonic, string Address) — direkt zuweisen.
                    result = await generator.GenerateAsync(progress, cts.Token);

                    task.StopTask();
                });

            // After the progress scope has finished (and been cleared), perform interactive output/prompts.
            if (!string.IsNullOrEmpty(result.Mnemonic) && !string.IsNullOrEmpty(result.Address))
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[green]√[/] Vanity address found!");

                var panel = new Panel(new Markup($"[yellow]{result.Mnemonic}[/]"))
                {
                    Header = new PanelHeader("[bold]Mnemonic Phrase[/]"),
                    Border = BoxBorder.Double
                };
                AnsiConsole.Write(panel);

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[bold]Address:[/] [green]{result.Address}[/]");
                AnsiConsole.MarkupLine($"[dim]Format: Bech32 (pocx1q...)[/]");

                // Generate WIF and descriptor for both mainnet and testnet
                var restoredWallet = HDWallet.FromMnemonic(result.Mnemonic);
                var wifMainnet = restoredWallet.GetWIFMainnet(0, 0);
                var wifTestnet = restoredWallet.GetWIFTestnet(0, 0);
                var descriptorMainnet = restoredWallet.GetDescriptorMainnet(0, 0);
                var descriptorTestnet = restoredWallet.GetDescriptorTestnet(0, 0);

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[bold]WIF Mainnet:[/] [dim]{wifMainnet}[/]");
                AnsiConsole.MarkupLine($"[bold]WIF Testnet:[/] [dim]{wifTestnet}[/]");

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[bold]Descriptor (Mainnet):[/] [dim]{descriptorMainnet}[/]");
                AnsiConsole.MarkupLine($"[bold]Descriptor (Testnet):[/] [dim]{descriptorTestnet}[/]");

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[bold red]▲ IMPORTANT: Save your mnemonic phrase in a secure location![/]");

                if (AnsiConsole.Confirm("Save to wallet file?", true))
                {
                    var filePath = AnsiConsole.Ask<string>("Enter file path:", "./vanity-wallet.json");

                    var walletData = new
                    {
                        mnemonic = result.Mnemonic,
                        address = result.Address,
                        wifMainnet = wifMainnet,
                        wifTestnet = wifTestnet,
                        descriptorMainnet = descriptorMainnet,
                        descriptorTestnet = descriptorTestnet,
                        pattern = pattern,
                        created = DateTime.UtcNow.ToString("o")
                    };

                    var json = System.Text.Json.JsonSerializer.Serialize(walletData, new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                    File.WriteAllText(filePath, json);
                    AnsiConsole.MarkupLine($"[green]√[/] Wallet saved to: {filePath}");
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]No result produced.[/]");
            }
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine("[yellow]Operation cancelled[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }

    private static bool IsValidBech32Pattern(string pattern)
    {
        return pattern.All(c => ValidBech32Chars.Contains(char.ToLower(c)));
    }
}

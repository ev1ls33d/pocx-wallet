using PocxWallet.Core.VanityAddress;
using Spectre.Console;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PocxWallet.Cli.Commands;

/// <summary>
/// Vanity address generation commands
/// </summary>
public static class VanityCommands
{
    public static async Task GenerateVanityAddressAsync()
    {
        AnsiConsole.MarkupLine("[bold green]Generate vanity address[/]");
        AnsiConsole.MarkupLine("[dim]Note: This may take a long time depending on the pattern complexity[/]");
        AnsiConsole.WriteLine();

        var pattern = AnsiConsole.Ask<string>("Enter [green]pattern[/] to search for:");
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
                AnsiConsole.MarkupLine("[green]✓ Vanity address found![/]");

                var panel = new Panel(new Markup($"[yellow]{result.Mnemonic}[/]"))
                {
                    Header = new PanelHeader("[bold]Mnemonic Phrase[/]"),
                    Border = BoxBorder.Double
                };
                AnsiConsole.Write(panel);

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[bold]Address:[/] [green]{result.Address}[/]");
                AnsiConsole.MarkupLine($"[dim]Format: Bech32 (pocx1q...)[/]");

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[bold red]⚠ IMPORTANT: Save your mnemonic phrase in a secure location![/]");

                if (AnsiConsole.Confirm("Save to wallet file?", true))
                {
                    var filePath = AnsiConsole.Ask<string>("Enter file path:", "./vanity-wallet.json");

                    var walletData = new
                    {
                        mnemonic = result.Mnemonic,
                        address = result.Address,
                        pattern = pattern,
                        created = DateTime.UtcNow.ToString("o")
                    };

                    var json = System.Text.Json.JsonSerializer.Serialize(walletData, new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                    File.WriteAllText(filePath, json);
                    AnsiConsole.MarkupLine($"[green]✓[/] Wallet saved to: {filePath}");
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
}

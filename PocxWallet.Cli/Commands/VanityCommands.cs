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

                // Generate WIF and descriptor for import
                var restoredWallet = HDWallet.FromMnemonic(result.Mnemonic);
                var derivedKey = restoredWallet.DeriveKeyForPoCX(0, 0);
                var wifKey = derivedKey.PrivateKey.GetWif(NBitcoin.Network.Main).ToString();
                var descriptorWithoutChecksum = $"wpkh({wifKey})";
                var checksum = CalculateDescriptorChecksum(descriptorWithoutChecksum);
                var descriptor = $"{descriptorWithoutChecksum}#{checksum}";

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[bold]WIF (for import):[/] [dim]{wifKey}[/]");
                AnsiConsole.MarkupLine($"[bold]Descriptor:[/] [dim]{descriptor}[/]");

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[bold red]▲ IMPORTANT: Save your mnemonic phrase in a secure location![/]");

                if (AnsiConsole.Confirm("Save to wallet file?", true))
                {
                    var filePath = AnsiConsole.Ask<string>("Enter file path:", "./vanity-wallet.json");

                    var walletData = new
                    {
                        mnemonic = result.Mnemonic,
                        address = result.Address,
                        wif = wifKey,
                        descriptor = descriptor,
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

    /// <summary>
    /// Calculate descriptor checksum according to BIP-380
    /// </summary>
    private static string CalculateDescriptorChecksum(string descriptor)
    {
        // Descriptor checksum uses a modified Bech32 charset
        const string CHECKSUM_CHARSET = "qpzry9x8gf2tvdw0s3jn54khce6mua7l";
        
        // Input characters mapping for descriptor
        const string INPUT_CHARSET = "0123456789()[],'/*abcdefgh@:$%{}IJKLMNOPQRSTUVWXYZ&+-.;<=>?!^_|~ijklmnopqrstuvwxyzABCDEFGH`#\"\\ ";
        
        // Polymod constants for descriptor checksum
        ulong[] GENERATOR = { 0xf5dee51989, 0xa9fdca3312, 0x1bab10e32d, 0x3706b1677a, 0x644d626ffd };
        
        // Expand the descriptor
        ulong c = 1;
        int cls = 0;
        int clscount = 0;
        
        foreach (char ch in descriptor)
        {
            int pos = INPUT_CHARSET.IndexOf(ch);
            if (pos == -1) continue;
            
            // Emit a symbol for the position inside the group, for every character.
            c = PolyMod(c, pos & 31, GENERATOR);
            
            // Accumulate the group numbers
            cls = cls * 3 + (pos >> 5);
            if (++clscount == 3)
            {
                // Emit an extra symbol representing the group numbers
                c = PolyMod(c, cls, GENERATOR);
                cls = 0;
                clscount = 0;
            }
        }
        
        if (clscount > 0)
        {
            c = PolyMod(c, cls, GENERATOR);
        }
        
        // Shift further to determine the checksum
        for (int j = 0; j < 8; ++j)
        {
            c = PolyMod(c, 0, GENERATOR);
        }
        
        // XOR with final constant
        c ^= 1;
        
        // Extract 8 5-bit groups for the checksum
        var checksum = new char[8];
        for (int j = 0; j < 8; ++j)
        {
            checksum[j] = CHECKSUM_CHARSET[(int)((c >> (5 * (7 - j))) & 31)];
        }
        
        return new string(checksum);
    }
    
    /// <summary>
    /// Polymod function for descriptor checksum calculation
    /// </summary>
    private static ulong PolyMod(ulong c, int val, ulong[] generator)
    {
        ulong c0 = c >> 35;
        c = ((c & 0x7ffffffff) << 5) ^ (ulong)val;
        
        for (int i = 0; i < 5; i++)
        {
            if (((c0 >> i) & 1) != 0)
            {
                c ^= generator[i];
            }
        }
        
        return c;
    }
}

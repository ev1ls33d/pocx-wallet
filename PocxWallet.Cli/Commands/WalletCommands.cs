using PocxWallet.Core.Wallet;
using PocxWallet.Core.VanityAddress;
using PocxWallet.Cli.Configuration;
using PocxWallet.Cli.Resources;
using Spectre.Console;
using NBitcoin;

namespace PocxWallet.Cli.Commands;

/// <summary>
/// Wallet-related commands with new menu structure
/// </summary>
public static class WalletCommands
{
    /// <summary>
    /// Shows the main wallet menu
    /// </summary>
    public static async Task ShowWalletMenuAsync(Action showBanner)
    {
        var walletManager = WalletManager.Instance;
        
        // Load wallet file if not loaded
        walletManager.Load();
        
        bool exit = false;
        while (!exit)
        {
            AnsiConsole.Clear();
            showBanner();
            
            // Build menu title with active wallet display
            var activeWallet = walletManager.ActiveWalletEntry;
            var title = activeWallet != null 
                ? string.Format(Strings.WalletMenu.TitleFormat, walletManager.GetActiveWalletDisplay())
                : Strings.WalletMenu.TitleNoWallet;
            
            var choices = new List<string>
            {
                Strings.WalletMenu.Create,
                Strings.WalletMenu.Switch,
                Strings.WalletMenu.Remove,
                Strings.WalletMenu.Info,
                Strings.WalletMenu.Transaction,
                Strings.ServiceMenu.Back
            };
            
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title(title)
                    .PageSize(10)
                    .AddChoices(choices)
            );
            
            AnsiConsole.Clear();
            showBanner();
            
            switch (choice)
            {
                case var c when c == Strings.WalletMenu.Create:
                    await ShowCreateMenuAsync(showBanner);
                    break;
                case var c when c == Strings.WalletMenu.Switch:
                    ShowSwitchMenu(showBanner);
                    break;
                case var c when c == Strings.WalletMenu.Remove:
                    ShowRemoveMenu(showBanner);
                    break;
                case var c when c == Strings.WalletMenu.Info:
                    await ShowInfoMenuAsync(showBanner);
                    break;
                case var c when c == Strings.WalletMenu.Transaction:
                    await ShowTransactionMenuAsync(showBanner);
                    break;
                case var c when c == Strings.ServiceMenu.Back:
                    exit = true;
                    break;
            }
        }
    }
    
    /// <summary>
    /// Shows the Create wallet submenu
    /// </summary>
    private static async Task ShowCreateMenuAsync(Action showBanner)
    {
        var choices = new List<string>
        {
            Strings.WalletMenu.RandomAddress,
            Strings.WalletMenu.VanityAddress,
            Strings.ServiceMenu.Back
        };
        
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold green]Create Wallet[/]")
                .PageSize(10)
                .AddChoices(choices)
        );
        
        AnsiConsole.Clear();
        showBanner();
        
        switch (choice)
        {
            case var c when c == Strings.WalletMenu.RandomAddress:
                await CreateRandomWalletAsync();
                break;
            case var c when c == Strings.WalletMenu.VanityAddress:
                await CreateVanityWalletAsync();
                break;
        }
    }
    
    /// <summary>
    /// Creates a random wallet from 12-word mnemonic
    /// </summary>
    private static async Task CreateRandomWalletAsync()
    {
        var walletManager = WalletManager.Instance;
        
        AnsiConsole.MarkupLine("[bold green]Creating new HD wallet with random mnemonic...[/]");
        AnsiConsole.WriteLine();
        
        // Ask for optional passphrase (hidden with asterisks)
        var passphrase = AnsiConsole.Prompt(
            new TextPrompt<string>(Strings.WalletMenu.EnterPassphrase)
                .AllowEmpty()
                .Secret());
        
        if (string.IsNullOrEmpty(passphrase))
            passphrase = null;
        
        // Generate 12-word mnemonic wallet
        var wallet = HDWallet.CreateNew(WordCount.Twelve, passphrase);
        
        // Display mnemonic and addresses
        DisplayWalletInfo(wallet);
        
        // Ask if wallet should be saved
        if (AnsiConsole.Confirm(Strings.WalletMenu.SaveWalletPrompt, true))
        {
            var filePath = AnsiConsole.Ask<string>(Strings.WalletMenu.EnterFilePath, "./wallet.json");
            
            // Ask for wallet name with uniqueness validation
            string walletName;
            while (true)
            {
                walletName = AnsiConsole.Ask<string>(Strings.WalletMenu.EnterWalletName, "default");
                
                if (walletManager.WalletNameExists(walletName))
                {
                    AnsiConsole.MarkupLine(string.Format(Strings.WalletMenu.WalletNameExists, walletName));
                    continue;
                }
                break;
            }
            
            // Add wallet to manager and save
            walletManager.AddWallet(wallet, walletName, passphrase, pattern: null, makeActive: true);
            walletManager.Save(filePath);
            
            AnsiConsole.MarkupLine(string.Format(Strings.WalletMenu.WalletCreated, walletName));
            AnsiConsole.MarkupLine(string.Format(Strings.WalletMenu.WalletSaved, filePath));
        }
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(Strings.WalletMenu.MnemonicWarning);
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(Strings.ServiceMenu.PressEnterToContinue);
        Console.ReadLine();
    }
    
    /// <summary>
    /// Creates a vanity address wallet
    /// </summary>
    private static async Task CreateVanityWalletAsync()
    {
        var walletManager = WalletManager.Instance;
        
        // Valid Bech32 characters (excluding '1', 'b', 'i', 'o' as per spec)
        const string ValidBech32Chars = "qpzry9x8gf2tvdw0s3jn54khce6mua7l";
        var validCharsSorted = string.Concat(ValidBech32Chars.OrderBy(c => c));
        
        AnsiConsole.MarkupLine("[bold green]Generate vanity address wallet[/]");
        AnsiConsole.MarkupLine("[dim]Note: This may take a long time depending on the pattern complexity[/]");
        AnsiConsole.MarkupLine($"[dim]Valid characters: {validCharsSorted} (case-insensitive)[/]");
        AnsiConsole.WriteLine();
        
        // Ask for search pattern with ! to cancel
        string pattern;
        while (true)
        {
            pattern = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter [green]pattern[/] to search for (or '!' to cancel):")
                    .AllowEmpty());
            
            if (pattern.Equals("!", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(pattern))
            {
                AnsiConsole.MarkupLine("[yellow]Operation cancelled[/]");
                return;
            }
            
            // Validate pattern
            if (!pattern.All(c => ValidBech32Chars.Contains(char.ToLower(c))))
            {
                AnsiConsole.MarkupLine("[red]Invalid pattern![/] Only these characters are allowed:");
                AnsiConsole.MarkupLine($"[yellow]{validCharsSorted}[/]");
                AnsiConsole.WriteLine();
                continue;
            }
            break;
        }
        
        var useTestnet = AnsiConsole.Confirm("Generate for [green]testnet[/]?", false);
        
        // Ask for optional passphrase (hidden with asterisks)
        var passphrase = AnsiConsole.Prompt(
            new TextPrompt<string>(Strings.WalletMenu.EnterPassphrase)
                .AllowEmpty()
                .Secret());
        
        if (string.IsNullOrEmpty(passphrase))
            passphrase = null;
        
        // Generate vanity address
        var generator = new VanityAddressGenerator(pattern, useTestnet);
        var cts = new CancellationTokenSource();
        
        (string Mnemonic, string Address) result = default;
        
        try
        {
            await AnsiConsole.Progress()
                .AutoRefresh(true)
                .AutoClear(true)
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
                    
                    result = await generator.GenerateAsync(progress, cts.Token);
                    task.StopTask();
                });
            
            if (!string.IsNullOrEmpty(result.Mnemonic) && !string.IsNullOrEmpty(result.Address))
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[green]✓[/] Vanity address found!");
                
                // Restore wallet with passphrase
                var wallet = HDWallet.FromMnemonic(result.Mnemonic, passphrase);
                
                // Display wallet info
                DisplayWalletInfo(wallet);
                
                // Ask if wallet should be saved
                if (AnsiConsole.Confirm(Strings.WalletMenu.SaveWalletPrompt, true))
                {
                    var filePath = AnsiConsole.Ask<string>(Strings.WalletMenu.EnterFilePath, "./wallet.json");
                    
                    // Ask for wallet name with uniqueness validation
                    string walletName;
                    while (true)
                    {
                        walletName = AnsiConsole.Ask<string>(Strings.WalletMenu.EnterWalletName, "vanity");
                        
                        if (walletManager.WalletNameExists(walletName))
                        {
                            AnsiConsole.MarkupLine(string.Format(Strings.WalletMenu.WalletNameExists, walletName));
                            continue;
                        }
                        break;
                    }
                    
                    // Add wallet to manager and save
                    walletManager.AddWallet(wallet, walletName, passphrase, pattern, makeActive: true);
                    walletManager.Save(filePath);
                    
                    AnsiConsole.MarkupLine(string.Format(Strings.WalletMenu.WalletCreated, walletName));
                    AnsiConsole.MarkupLine(string.Format(Strings.WalletMenu.WalletSaved, filePath));
                }
                
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine(Strings.WalletMenu.MnemonicWarning);
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
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(Strings.ServiceMenu.PressEnterToContinue);
        Console.ReadLine();
    }
    
    /// <summary>
    /// Displays wallet info (mnemonic, addresses, WIF, descriptor)
    /// </summary>
    private static void DisplayWalletInfo(HDWallet wallet)
    {
        AnsiConsole.WriteLine();
        var panel = new Panel(new Markup($"[yellow]{wallet.MnemonicPhrase}[/]"))
        {
            Header = new PanelHeader("[bold]Mnemonic Phrase[/]"),
            Border = BoxBorder.Double
        };
        AnsiConsole.Write(panel);
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold]Mainnet Address:[/] [green]{wallet.GetPoCXAddress(0, 0, testnet: false)}[/]");
        AnsiConsole.MarkupLine($"[bold]Testnet Address:[/] [green]{wallet.GetPoCXAddress(0, 0, testnet: true)}[/]");
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold]WIF Mainnet:[/] [dim]{wallet.GetWIFMainnet()}[/]");
        AnsiConsole.MarkupLine($"[bold]WIF Testnet:[/] [dim]{wallet.GetWIFTestnet()}[/]");
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold]Descriptor (Mainnet):[/] [dim]{wallet.GetDescriptor()}[/]");
        AnsiConsole.MarkupLine($"[bold]Descriptor (Testnet):[/] [dim]{wallet.GetDescriptor(true)}[/]");
    }
    
    /// <summary>
    /// Shows the Switch wallet submenu
    /// </summary>
    private static void ShowSwitchMenu(Action showBanner)
    {
        var walletManager = WalletManager.Instance;
        
        if (walletManager.Wallets.Count == 0)
        {
            AnsiConsole.MarkupLine(Strings.WalletMenu.NoWalletsAvailable);
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine(Strings.ServiceMenu.PressEnterToContinue);
            Console.ReadLine();
            return;
        }
        
        // Build wallet choices with name and first 15 chars of mainnet address
        var choices = new List<string>();
        foreach (var wallet in walletManager.Wallets)
        {
            var addressPrefix = wallet.MainnetAddress.Length > 15 
                ? wallet.MainnetAddress[..15] + "..."
                : wallet.MainnetAddress;
            var label = $"{wallet.Name.PadRight(15)} {addressPrefix}";
            
            // Mark active wallet
            if (wallet.Name == walletManager.ActiveWalletEntry?.Name)
                label = $"[green]●[/] {label}";
            else
                label = $"  {label}";
            
            choices.Add(label);
        }
        choices.Add(Strings.ServiceMenu.Back);
        
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold green]Switch Wallet[/]")
                .PageSize(15)
                .AddChoices(choices)
        );
        
        if (choice == Strings.ServiceMenu.Back)
            return;
        
        // Extract wallet name from choice
        var walletName = choice.TrimStart('●', ' ').Split(' ')[0].Trim();
        
        if (walletManager.SwitchWallet(walletName))
        {
            walletManager.Save();
            AnsiConsole.MarkupLine(string.Format(Strings.WalletMenu.SwitchedTo, walletName));
        }
        else
        {
            AnsiConsole.MarkupLine(Strings.WalletMenu.SwitchFailed);
        }
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(Strings.ServiceMenu.PressEnterToContinue);
        Console.ReadLine();
    }
    
    /// <summary>
    /// Shows the Remove wallet submenu
    /// </summary>
    private static void ShowRemoveMenu(Action showBanner)
    {
        var walletManager = WalletManager.Instance;
        
        if (walletManager.Wallets.Count == 0)
        {
            AnsiConsole.MarkupLine(Strings.WalletMenu.NoWalletsAvailable);
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine(Strings.ServiceMenu.PressEnterToContinue);
            Console.ReadLine();
            return;
        }
        
        // Build wallet choices
        var choices = new List<string>();
        foreach (var wallet in walletManager.Wallets)
        {
            var addressPrefix = wallet.MainnetAddress.Length > 15 
                ? wallet.MainnetAddress[..15] + "..."
                : wallet.MainnetAddress;
            var label = $"{wallet.Name.PadRight(15)} {addressPrefix}";
            choices.Add(label);
        }
        choices.Add(Strings.ServiceMenu.Back);
        
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold green]Remove Wallet[/]")
                .PageSize(15)
                .AddChoices(choices)
        );
        
        if (choice == Strings.ServiceMenu.Back)
            return;
        
        // Extract wallet name from choice
        var walletName = choice.Split(' ')[0].Trim();
        
        // Confirm removal
        if (!AnsiConsole.Confirm(string.Format(Strings.WalletMenu.ConfirmRemove, walletName), false))
            return;
        
        // Ask about node unloading
        if (AnsiConsole.Confirm(Strings.WalletMenu.UnloadFromNode, false))
        {
            // Show command template for node unload (stub)
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine(Strings.WalletMenu.CommandTemplateHeader);
            AnsiConsole.MarkupLine(string.Format(Strings.WalletMenu.UnloadNotImplemented, walletName));
        }
        
        // Remove wallet
        if (walletManager.RemoveWallet(walletName))
        {
            walletManager.Save();
            AnsiConsole.MarkupLine(string.Format(Strings.WalletMenu.WalletRemoved, walletName));
        }
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(Strings.ServiceMenu.PressEnterToContinue);
        Console.ReadLine();
    }
    
    /// <summary>
    /// Shows the Info submenu with bitcoin-cli command stubs
    /// </summary>
    private static async Task ShowInfoMenuAsync(Action showBanner)
    {
        var walletManager = WalletManager.Instance;
        var activeWallet = walletManager.ActiveWalletEntry;
        var walletName = activeWallet?.Name ?? "mywallet";
        
        var choices = new List<string>
        {
            Strings.WalletMenu.CheckBalance,
            Strings.WalletMenu.ShowAddresses,
            Strings.WalletMenu.ListUnspent,
            Strings.WalletMenu.GetWalletInfo,
            Strings.WalletMenu.GetBlockchainInfo,
            Strings.WalletMenu.TransactionHistory,
            Strings.ServiceMenu.Back
        };
        
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold green]Wallet Info[/]")
                .PageSize(10)
                .AddChoices(choices)
        );
        
        if (choice == Strings.ServiceMenu.Back)
            return;
        
        AnsiConsole.Clear();
        showBanner();
        
        // Show command template based on selection
        string command = choice switch
        {
            var c when c == Strings.WalletMenu.CheckBalance => 
                $"docker exec <container> bitcoin-cli -wallet={walletName} getbalance",
            var c when c == Strings.WalletMenu.ShowAddresses => 
                $"docker exec <container> bitcoin-cli -wallet={walletName} listreceivedbyaddress 0 true",
            var c when c == Strings.WalletMenu.ListUnspent => 
                $"docker exec <container> bitcoin-cli -wallet={walletName} listunspent",
            var c when c == Strings.WalletMenu.GetWalletInfo => 
                $"docker exec <container> bitcoin-cli -wallet={walletName} getwalletinfo",
            var c when c == Strings.WalletMenu.GetBlockchainInfo => 
                "docker exec <container> bitcoin-cli getblockchaininfo",
            var c when c == Strings.WalletMenu.TransactionHistory => 
                $"docker exec <container> bitcoin-cli -wallet={walletName} listtransactions \"*\" 10",
            _ => ""
        };
        
        ShowCommandTemplate(choice, command);
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(Strings.ServiceMenu.PressEnterToContinue);
        Console.ReadLine();
    }
    
    /// <summary>
    /// Shows the Transaction submenu with bitcoin-cli command stubs
    /// </summary>
    private static async Task ShowTransactionMenuAsync(Action showBanner)
    {
        var walletManager = WalletManager.Instance;
        var activeWallet = walletManager.ActiveWalletEntry;
        var walletName = activeWallet?.Name ?? "mywallet";
        
        var choices = new List<string>
        {
            Strings.WalletMenu.SendFunds,
            Strings.WalletMenu.CreateTransaction,
            Strings.WalletMenu.SignTransaction,
            Strings.WalletMenu.BroadcastTransaction,
            Strings.WalletMenu.CreatePSBT,
            Strings.WalletMenu.DecodePSBT,
            Strings.ServiceMenu.Back
        };
        
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold green]Transactions[/]")
                .PageSize(10)
                .AddChoices(choices)
        );
        
        if (choice == Strings.ServiceMenu.Back)
            return;
        
        AnsiConsole.Clear();
        showBanner();
        
        // Show command template based on selection
        string command = choice switch
        {
            var c when c == Strings.WalletMenu.SendFunds => 
                $"docker exec <container> bitcoin-cli -wallet={walletName} sendtoaddress \"<address>\" <amount>",
            var c when c == Strings.WalletMenu.CreateTransaction => 
                $"docker exec <container> bitcoin-cli -wallet={walletName} createrawtransaction '[{{\"txid\":\"...\",\"vout\":0}}]' '{{\"<address>\":<amount>}}'",
            var c when c == Strings.WalletMenu.SignTransaction => 
                $"docker exec <container> bitcoin-cli -wallet={walletName} signrawtransactionwithwallet \"<hex>\"",
            var c when c == Strings.WalletMenu.BroadcastTransaction => 
                "docker exec <container> bitcoin-cli sendrawtransaction \"<hex>\"",
            var c when c == Strings.WalletMenu.CreatePSBT => 
                $"docker exec <container> bitcoin-cli -wallet={walletName} walletcreatefundedpsbt '[]' '{{\"<address>\":<amount>}}'",
            var c when c == Strings.WalletMenu.DecodePSBT => 
                "docker exec <container> bitcoin-cli decodepsbt \"<psbt>\"",
            _ => ""
        };
        
        ShowCommandTemplate(choice, command);
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(Strings.ServiceMenu.PressEnterToContinue);
        Console.ReadLine();
    }
    
    /// <summary>
    /// Displays a command template for manual copy/paste
    /// </summary>
    private static void ShowCommandTemplate(string actionName, string command)
    {
        AnsiConsole.MarkupLine($"[bold]{actionName}[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(Strings.WalletMenu.CommandTemplateHeader);
        AnsiConsole.WriteLine();
        
        var panel = new Panel(new Markup($"[dim]{Markup.Escape(command)}[/]"))
        {
            Border = BoxBorder.Rounded,
            Padding = new Padding(1, 0, 1, 0)
        };
        AnsiConsole.Write(panel);
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(Strings.WalletMenu.CopyPasteNote);
    }
    
    // ========================================
    // Legacy methods (kept for compatibility)
    // ========================================
    
    public static async Task CreateNewWallet()
    {
        await CreateRandomWalletAsync();
    }

    public static async Task RestoreWallet()
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
            AnsiConsole.MarkupLine("[green]✓[/] Wallet restored successfully!");
            DisplayWalletInfo(wallet);

            if (AnsiConsole.Confirm("Save wallet to file?", true))
            {
                var walletManager = WalletManager.Instance;
                var filePath = AnsiConsole.Ask<string>("Enter file path:", "./wallet.json");
                
                string walletName;
                while (true)
                {
                    walletName = AnsiConsole.Ask<string>(Strings.WalletMenu.EnterWalletName, "restored");
                    
                    if (walletManager.WalletNameExists(walletName))
                    {
                        AnsiConsole.MarkupLine(string.Format(Strings.WalletMenu.WalletNameExists, walletName));
                        continue;
                    }
                    break;
                }
                
                walletManager.AddWallet(wallet, walletName, passphrase, makeActive: true);
                walletManager.Save(filePath);
                AnsiConsole.MarkupLine($"[green]✓[/] Wallet saved to: {filePath}");
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }

    /// <summary>
    /// Import wallet from wallet.json file into Bitcoin node (called from Node menu)
    /// </summary>
    public static async Task ImportWalletFromFileAsync(AppSettings settings)
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
            
            var mnemonicElement = walletData.RootElement.GetProperty("mnemonic");
            var mnemonic = mnemonicElement.GetString();

            if (string.IsNullOrEmpty(mnemonic))
            {
                AnsiConsole.MarkupLine("[red]Invalid wallet file - mnemonic not found![/]");
                return;
            }

            // Try to get passphrase from wallet file
            string? passphrase = null;
            if (walletData.RootElement.TryGetProperty("passphrase", out var passphraseElement))
            {
                passphrase = passphraseElement.GetString();
                if (!string.IsNullOrEmpty(passphrase))
                {
                    AnsiConsole.MarkupLine("[yellow]Wallet has a passphrase - it will be used for restoration[/]");
                }
            }

            var wallet = HDWallet.FromMnemonic(mnemonic, passphrase);
            
            // Detect network mode from node settings
            var isTestnet = settings.BitcoinNode.AdditionalParams.Contains("-testnet", StringComparison.OrdinalIgnoreCase);
            var networkName = isTestnet ? "testnet" : "mainnet";
            
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[bold]Detected network mode:[/] [green]{networkName}[/]");
            if (isTestnet)
            {
                AnsiConsole.MarkupLine($"[dim](detected from -testnet flag in node parameters)[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[dim](mainnet - no -testnet flag found in node parameters)[/]");
            };
            
            var walletName = AnsiConsole.Ask<string>("Enter wallet name for Bitcoin node:", "pocx_wallet");
            
            var wif = wallet.GetDescriptor(isTestnet);
            var address = wallet.GetPoCXAddress(0, 0, isTestnet);

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[bold]Importing wallet:[/] {Markup.Escape(walletName)}");
            AnsiConsole.MarkupLine($"[bold]Network:[/] {networkName}");
            AnsiConsole.MarkupLine($"[bold]Address:[/] {Markup.Escape(address)}");
            AnsiConsole.WriteLine();

            var success = await NodeCommands.ImportWalletFromWIFAsync(settings, walletName, wif, address, isTestnet);
            
            if (success)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[green]✓[/] Wallet successfully imported into Bitcoin node!");
                AnsiConsole.MarkupLine("[dim]You can now use the wallet for transactions through the node[/]");
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
                table.AddRow(i.ToString(), address, Markup.Escape(pubKey[..16] + "..."));
            }

            AnsiConsole.Write(table);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }
}

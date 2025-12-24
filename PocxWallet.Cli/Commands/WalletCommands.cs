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
    public static async Task ShowWalletMenuAsync(Action showBanner, Func<string, Task<bool>>? isNodeRunningAsync = null, Func<string, string, Task<(int, string)>>? execInContainerAsync = null)
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
                Strings.WalletMenu.Settings,
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
                    await ShowCreateMenuAsync(showBanner, isNodeRunningAsync, execInContainerAsync);
                    break;
                case var c when c == Strings.WalletMenu.Switch:
                    ShowSwitchMenu(showBanner);
                    break;
                case var c when c == Strings.WalletMenu.Remove:
                    await ShowRemoveMenuAsync(showBanner, isNodeRunningAsync, execInContainerAsync);
                    break;
                case var c when c == Strings.WalletMenu.Info:
                    await ShowInfoMenuAsync(showBanner, isNodeRunningAsync, execInContainerAsync);
                    break;
                case var c when c == Strings.WalletMenu.Transaction:
                    await ShowTransactionMenuAsync(showBanner, isNodeRunningAsync, execInContainerAsync);
                    break;
                case var c when c == Strings.WalletMenu.Settings:
                    ShowWalletSettingsMenu(showBanner);
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
    private static async Task ShowCreateMenuAsync(Action showBanner, Func<string, Task<bool>>? isNodeRunningAsync, Func<string, string, Task<(int, string)>>? execInContainerAsync)
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
                await CreateRandomWalletAsync(isNodeRunningAsync, execInContainerAsync);
                break;
            case var c when c == Strings.WalletMenu.VanityAddress:
                await CreateVanityWalletAsync(isNodeRunningAsync, execInContainerAsync);
                break;
        }
    }
    
    /// <summary>
    /// Creates a random wallet from 12-word mnemonic
    /// </summary>
    private static async Task CreateRandomWalletAsync(Func<string, Task<bool>>? isNodeRunningAsync, Func<string, string, Task<(int, string)>>? execInContainerAsync)
    {
        var walletManager = WalletManager.Instance;
        var settings = walletManager.Settings;
        
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
        
        // Check settings for auto-save, otherwise ask
        bool shouldSave = settings.AutoSave || AnsiConsole.Confirm(Strings.WalletMenu.SaveWalletPrompt, true);
        
        if (shouldSave)
        {
            var filePath = settings.AutoSave && !string.IsNullOrEmpty(settings.DefaultWalletPath)
                ? settings.DefaultWalletPath
                : AnsiConsole.Ask<string>(Strings.WalletMenu.EnterFilePath, settings.DefaultWalletPath);
            
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
            
            // Ask to import to node
            bool shouldImport = settings.AutoImportToNode || AnsiConsole.Confirm(Strings.WalletMenu.ImportToNodePrompt, false);
            if (shouldImport)
            {
                await ImportWalletToNodeAsync(wallet, walletName, isNodeRunningAsync, execInContainerAsync);
            }
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
    private static async Task CreateVanityWalletAsync(Func<string, Task<bool>>? isNodeRunningAsync, Func<string, string, Task<(int, string)>>? execInContainerAsync)
    {
        var walletManager = WalletManager.Instance;
        var settings = walletManager.Settings;
        
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
                
                // Check settings for auto-save, otherwise ask
                bool shouldSave = settings.AutoSave || AnsiConsole.Confirm(Strings.WalletMenu.SaveWalletPrompt, true);
                
                if (shouldSave)
                {
                    var filePath = settings.AutoSave && !string.IsNullOrEmpty(settings.DefaultWalletPath)
                        ? settings.DefaultWalletPath
                        : AnsiConsole.Ask<string>(Strings.WalletMenu.EnterFilePath, settings.DefaultWalletPath);
                    
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
                    
                    // Ask to import to node
                    bool shouldImport = settings.AutoImportToNode || AnsiConsole.Confirm(Strings.WalletMenu.ImportToNodePrompt, false);
                    if (shouldImport)
                    {
                        await ImportWalletToNodeAsync(wallet, walletName, isNodeRunningAsync, execInContainerAsync);
                    }
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
        
        // Build wallet choices with name and full mainnet address
        var choices = new List<string>();
        var choiceToWalletName = new Dictionary<string, string>();
        
        foreach (var wallet in walletManager.Wallets)
        {
            var label = $"{wallet.Name.PadRight(15)} {wallet.MainnetAddress}";
            
            // Mark active wallet
            if (wallet.Name == walletManager.ActiveWalletEntry?.Name)
                label = $"[green]●[/] {label}";
            else
                label = $"  {label}";
            
            choices.Add(label);
            choiceToWalletName[label] = wallet.Name;
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
        
        // Look up wallet name from dictionary
        if (choiceToWalletName.TryGetValue(choice, out var walletName))
        {
            if (walletManager.SwitchWallet(walletName))
            {
                walletManager.Save();
                AnsiConsole.MarkupLine(string.Format(Strings.WalletMenu.SwitchedTo, walletName));
            }
            else
            {
                AnsiConsole.MarkupLine(Strings.WalletMenu.SwitchFailed);
            }
        }
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(Strings.ServiceMenu.PressEnterToContinue);
        Console.ReadLine();
    }
    
    /// <summary>
    /// Shows the Remove wallet submenu
    /// </summary>
    private static async Task ShowRemoveMenuAsync(Action showBanner, Func<string, Task<bool>>? isNodeRunningAsync, Func<string, string, Task<(int, string)>>? execInContainerAsync)
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
        
        // Build wallet choices with dictionary for reliable name lookup
        var choices = new List<string>();
        var choiceToWalletName = new Dictionary<string, string>();
        
        foreach (var wallet in walletManager.Wallets)
        {
            var label = $"{wallet.Name.PadRight(15)} {wallet.MainnetAddress}";
            choices.Add(label);
            choiceToWalletName[label] = wallet.Name;
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
        
        // Look up wallet name from dictionary
        if (!choiceToWalletName.TryGetValue(choice, out var walletName))
            return;
        
        // Confirm removal
        if (!AnsiConsole.Confirm(string.Format(Strings.WalletMenu.ConfirmRemove, walletName), false))
            return;
        
        // Ask about node unloading
        if (AnsiConsole.Confirm(Strings.WalletMenu.UnloadFromNode, false))
        {
            var command = $"bitcoin-cli unloadwallet \"{walletName}\"";
            await ExecuteNodeCommandAsync(command, isNodeRunningAsync, execInContainerAsync);
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
    private static async Task ShowInfoMenuAsync(Action showBanner, Func<string, Task<bool>>? isNodeRunningAsync, Func<string, string, Task<(int, string)>>? execInContainerAsync)
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
        
        // Build command based on selection
        string command = choice switch
        {
            var c when c == Strings.WalletMenu.CheckBalance => 
                $"bitcoin-cli -wallet={walletName} getbalance",
            var c when c == Strings.WalletMenu.ShowAddresses => 
                $"bitcoin-cli -wallet={walletName} listreceivedbyaddress 0 true",
            var c when c == Strings.WalletMenu.ListUnspent => 
                $"bitcoin-cli -wallet={walletName} listunspent",
            var c when c == Strings.WalletMenu.GetWalletInfo => 
                $"bitcoin-cli -wallet={walletName} getwalletinfo",
            var c when c == Strings.WalletMenu.GetBlockchainInfo => 
                "bitcoin-cli getblockchaininfo",
            var c when c == Strings.WalletMenu.TransactionHistory => 
                $"bitcoin-cli -wallet={walletName} listtransactions \"*\" 10",
            _ => ""
        };
        
        await ExecuteNodeCommandAsync(command, isNodeRunningAsync, execInContainerAsync);
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(Strings.ServiceMenu.PressEnterToContinue);
        Console.ReadLine();
    }
    
    /// <summary>
    /// Shows the Transaction submenu with bitcoin-cli command stubs
    /// </summary>
    private static async Task ShowTransactionMenuAsync(Action showBanner, Func<string, Task<bool>>? isNodeRunningAsync, Func<string, string, Task<(int, string)>>? execInContainerAsync)
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
        
        // Build command based on selection (these are templates that need user input)
        string command = choice switch
        {
            var c when c == Strings.WalletMenu.SendFunds => 
                $"bitcoin-cli -wallet={walletName} sendtoaddress \"<address>\" <amount>",
            var c when c == Strings.WalletMenu.CreateTransaction => 
                $"bitcoin-cli -wallet={walletName} createrawtransaction '[{{\"txid\":\"...\",\"vout\":0}}]' '{{\"<address>\":<amount>}}'",
            var c when c == Strings.WalletMenu.SignTransaction => 
                $"bitcoin-cli -wallet={walletName} signrawtransactionwithwallet \"<hex>\"",
            var c when c == Strings.WalletMenu.BroadcastTransaction => 
                "bitcoin-cli sendrawtransaction \"<hex>\"",
            var c when c == Strings.WalletMenu.CreatePSBT => 
                $"bitcoin-cli -wallet={walletName} walletcreatefundedpsbt '[]' '{{\"<address>\":<amount>}}'",
            var c when c == Strings.WalletMenu.DecodePSBT => 
                "bitcoin-cli decodepsbt \"<psbt>\"",
            _ => ""
        };
        
        // Transaction commands are templates - show copy/paste for now
        ShowCommandTemplate(choice, $"docker exec <container> {command}");
        
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
    
    /// <summary>
    /// Execute a command on the node container if running, otherwise show copy/paste template
    /// </summary>
    private static async Task ExecuteNodeCommandAsync(string command, Func<string, Task<bool>>? isNodeRunningAsync, Func<string, string, Task<(int, string)>>? execInContainerAsync)
    {
        const string containerName = "pocx-bitcoin-node";
        
        // Check if node is running
        bool nodeRunning = false;
        if (isNodeRunningAsync != null)
        {
            nodeRunning = await isNodeRunningAsync(containerName);
        }
        
        if (nodeRunning && execInContainerAsync != null)
        {
            // Node is running - ask if should execute
            if (AnsiConsole.Confirm(Strings.WalletMenu.ExecuteOnNodePrompt, true))
            {
                AnsiConsole.MarkupLine($"[dim]Executing: {Markup.Escape(command)}[/]");
                AnsiConsole.WriteLine();
                
                var (exitCode, output) = await execInContainerAsync(containerName, command);
                
                // Show output
                if (!string.IsNullOrWhiteSpace(output))
                {
                    AnsiConsole.MarkupLine("[bold]Output:[/]");
                    var panel = new Panel(Markup.Escape(output))
                    {
                        Border = BoxBorder.Rounded,
                        Padding = new Padding(1, 0, 1, 0)
                    };
                    AnsiConsole.Write(panel);
                }
                
                if (exitCode == 0)
                {
                    AnsiConsole.MarkupLine("[green]✓[/] Command completed successfully");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[yellow]⚠[/] Command exited with code {exitCode}");
                }
                
                // Show last 10 log lines
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[bold]Last 10 log lines:[/]");
                var (_, logs) = await execInContainerAsync(containerName, "tail -n 10 /root/.bitcoin/debug.log 2>/dev/null || echo 'No log file available'");
                if (!string.IsNullOrWhiteSpace(logs))
                {
                    AnsiConsole.MarkupLine($"[dim]{Markup.Escape(logs)}[/]");
                }
            }
            else
            {
                ShowCommandTemplate("Command", $"docker exec {containerName} {command}");
            }
        }
        else
        {
            // Node is not running
            if (AnsiConsole.Confirm(Strings.WalletMenu.NodeNotRunningStartPrompt, false))
            {
                AnsiConsole.MarkupLine("[yellow]Please start the node from the main menu, then return here.[/]");
            }
            else
            {
                ShowCommandTemplate("Command", $"docker exec {containerName} {command}");
            }
        }
    }
    
    /// <summary>
    /// Import wallet to the Bitcoin node
    /// </summary>
    private static async Task ImportWalletToNodeAsync(HDWallet wallet, string walletName, Func<string, Task<bool>>? isNodeRunningAsync, Func<string, string, Task<(int, string)>>? execInContainerAsync)
    {
        const string containerName = "pocx-bitcoin-node";
        
        // Check if node is running
        bool nodeRunning = false;
        if (isNodeRunningAsync != null)
        {
            nodeRunning = await isNodeRunningAsync(containerName);
        }
        
        if (!nodeRunning)
        {
            if (!AnsiConsole.Confirm(Strings.WalletMenu.NodeNotRunningStartPrompt, false))
            {
                AnsiConsole.MarkupLine("[yellow]Skipping import to node.[/]");
                return;
            }
            AnsiConsole.MarkupLine("[yellow]Please start the node from the main menu, then import manually.[/]");
            return;
        }
        
        if (execInContainerAsync == null)
        {
            AnsiConsole.MarkupLine("[yellow]Node execution not available. Import manually using bitcoin-cli.[/]");
            return;
        }
        
        AnsiConsole.MarkupLine("[bold]Importing wallet to Bitcoin node...[/]");
        
        // Detect network (testnet for now, could be made configurable)
        var isTestnet = true;  // TODO: detect from node parameters
        var descriptor = wallet.GetDescriptor(isTestnet);
        
        // Step 1: Create wallet on node
        AnsiConsole.MarkupLine("[dim]Creating wallet on node...[/]");
        var createCmd = $"bitcoin-cli createwallet \"{walletName}\" false false \"\" false true";
        var (createExitCode, createOutput) = await execInContainerAsync(containerName, createCmd);
        
        if (createExitCode != 0 && !createOutput.Contains("already exists"))
        {
            AnsiConsole.MarkupLine($"[yellow]⚠[/] Create wallet: {Markup.Escape(createOutput)}");
        }
        else
        {
            AnsiConsole.MarkupLine("[green]✓[/] Wallet created on node");
        }
        
        // Step 2: Import descriptor
        AnsiConsole.MarkupLine("[dim]Importing descriptor...[/]");
        var importJson = $"'[{{\"desc\": \"{descriptor}\", \"timestamp\": \"now\"}}]'";
        var importCmd = $"bitcoin-cli -wallet={walletName} importdescriptors {importJson}";
        var (importExitCode, importOutput) = await execInContainerAsync(containerName, importCmd);
        
        if (importExitCode == 0)
        {
            AnsiConsole.MarkupLine("[green]✓[/] Descriptor imported successfully");
        }
        else
        {
            AnsiConsole.MarkupLine($"[yellow]⚠[/] Import result: {Markup.Escape(importOutput)}");
        }
    }
    
    /// <summary>
    /// Shows the wallet settings menu
    /// </summary>
    private static void ShowWalletSettingsMenu(Action showBanner)
    {
        var walletManager = WalletManager.Instance;
        var settings = walletManager.Settings;
        
        bool back = false;
        while (!back)
        {
            AnsiConsole.Clear();
            showBanner();
            
            var choices = new List<string>
            {
                $"{"Default Wallet Path".PadRight(25)} [cyan]{Markup.Escape(settings.DefaultWalletPath)}[/]",
                $"{"Auto-Save Wallets".PadRight(25)} {(settings.AutoSave ? "[green]true[/]" : "[red]false[/]")}",
                $"{"Startup Wallet".PadRight(25)} [cyan]{Markup.Escape(settings.StartupWallet ?? "(none)")}[/]",
                $"{"Auto-Import to Node".PadRight(25)} {(settings.AutoImportToNode ? "[green]true[/]" : "[red]false[/]")}",
                Strings.ServiceMenu.Back
            };
            
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold green]Wallet Settings[/]")
                    .PageSize(10)
                    .AddChoices(choices)
            );
            
            if (choice == Strings.ServiceMenu.Back)
            {
                back = true;
                continue;
            }
            
            // Determine which setting was selected
            var index = choices.IndexOf(choice);
            switch (index)
            {
                case 0: // Default Wallet Path
                    settings.DefaultWalletPath = AnsiConsole.Ask("Enter default wallet path:", settings.DefaultWalletPath);
                    walletManager.Save();
                    AnsiConsole.MarkupLine("[green]✓[/] Setting updated");
                    break;
                    
                case 1: // Auto-Save
                    settings.AutoSave = !settings.AutoSave;
                    walletManager.Save();
                    AnsiConsole.MarkupLine($"[green]✓[/] Auto-save is now {(settings.AutoSave ? "enabled" : "disabled")}");
                    break;
                    
                case 2: // Startup Wallet
                    if (walletManager.Wallets.Count > 0)
                    {
                        var walletChoices = walletManager.Wallets.Select(w => w.Name).ToList();
                        walletChoices.Insert(0, "(none)");
                        var selected = AnsiConsole.Prompt(
                            new SelectionPrompt<string>()
                                .Title("Select startup wallet:")
                                .AddChoices(walletChoices)
                        );
                        settings.StartupWallet = selected == "(none)" ? null : selected;
                        walletManager.Save();
                        AnsiConsole.MarkupLine("[green]✓[/] Setting updated");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[yellow]No wallets available. Create a wallet first.[/]");
                    }
                    break;
                    
                case 3: // Auto-Import to Node
                    settings.AutoImportToNode = !settings.AutoImportToNode;
                    walletManager.Save();
                    AnsiConsole.MarkupLine($"[green]✓[/] Auto-import is now {(settings.AutoImportToNode ? "enabled" : "disabled")}");
                    break;
            }
            
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine(Strings.ServiceMenu.PressEnterToContinue);
            Console.ReadLine();
        }
    }
    
    // ========================================
    // Legacy methods (kept for compatibility)
    // ========================================
    
    public static async Task CreateNewWallet()
    {
        await CreateRandomWalletAsync(null, null);
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

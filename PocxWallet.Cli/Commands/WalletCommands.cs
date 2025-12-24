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
    // Static fields for node integration
    private static string _bitcoinContainerName = "pocx-node";
    private static Func<Task<bool>>? _isNodeRunningAsync;
    private static Func<string, Task<(int, string)>>? _execInContainerAsync;
    private static Func<Task<bool>>? _startNodeAsync;
    private static ServiceConfiguration? _serviceConfig;
    
    /// <summary>
    /// Check if the node is configured for testnet by checking the parameters in services.yaml
    /// </summary>
    private static bool IsNodeTestnet()
    {
        // Load service configuration if not already loaded
        _serviceConfig ??= ServiceDefinitionLoader.LoadServices();
        
        if (_serviceConfig?.Services == null)
            // Default to testnet for safety - mainnet transactions are irreversible and costly,
            // so in case of configuration issues, testnet is the safer default to prevent accidental mainnet use
            return true;
        
        // Find the node service
        var nodeService = _serviceConfig.Services.FirstOrDefault(s => 
            s.Id?.Equals("node", StringComparison.OrdinalIgnoreCase) == true ||
            s.Id?.Equals("bitcoin-node", StringComparison.OrdinalIgnoreCase) == true);
        
        if (nodeService?.Parameters == null)
            // Default to testnet for safety - mainnet operations should be explicitly configured
            return true;
        
        // Check if testnet parameter has a value set
        var testnetParam = nodeService.Parameters.FirstOrDefault(p => 
            p.Name?.Equals("testnet", StringComparison.OrdinalIgnoreCase) == true);
        
        if (testnetParam != null)
        {
            // If value is explicitly set, use it
            if (testnetParam.Value != null)
            {
                if (bool.TryParse(testnetParam.Value.ToString(), out var isTestnet))
                    return isTestnet;
            }
            // Otherwise use the default
            if (testnetParam.Default != null)
            {
                if (bool.TryParse(testnetParam.Default.ToString(), out var defaultTestnet))
                    return defaultTestnet;
            }
        }
        
        return true; // Default to testnet for safety
    }
    
    /// <summary>
    /// Shows the main wallet menu
    /// </summary>
    public static async Task ShowWalletMenuAsync(
        Action showBanner, 
        string? bitcoinContainerName = null,
        Func<Task<bool>>? isNodeRunningAsync = null, 
        Func<string, Task<(int, string)>>? execInContainerAsync = null,
        Func<Task<bool>>? startNodeAsync = null)
    {
        var walletManager = WalletManager.Instance;
        
        // Store container name for use in helper methods
        _bitcoinContainerName = bitcoinContainerName ?? "pocx-node";
        _isNodeRunningAsync = isNodeRunningAsync;
        _execInContainerAsync = execInContainerAsync;
        _startNodeAsync = startNodeAsync;
        
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
                Strings.WalletMenu.Manage,
                Strings.WalletMenu.Select,
                Strings.WalletMenu.Info,
                Strings.WalletMenu.Transaction,
                Strings.WalletMenu.Settings,
                Strings.ServiceMenu.Back
            };
            
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title(title)
                    .PageSize(12)
                    .AddChoices(choices)
            );
            
            AnsiConsole.Clear();
            showBanner();
            
            switch (choice)
            {
                case var c when c == Strings.WalletMenu.Manage:
                    await ShowManageMenuAsync(showBanner);
                    break;
                case var c when c == Strings.WalletMenu.Select:
                    ShowSelectMenu(showBanner);
                    break;
                case var c when c == Strings.WalletMenu.Info:
                    await ShowInfoMenuAsync(showBanner);
                    break;
                case var c when c == Strings.WalletMenu.Transaction:
                    await ShowTransactionMenuAsync(showBanner);
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
    /// Shows the Manage wallet submenu (contains Create, Import, Remove)
    /// </summary>
    private static async Task ShowManageMenuAsync(Action showBanner)
    {
        var choices = new List<string>
        {
            Strings.WalletMenu.Create,
            Strings.WalletMenu.Import,
            Strings.WalletMenu.Remove,
            Strings.ServiceMenu.Back
        };
        
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title(Strings.WalletMenu.ManageMenuTitle)
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
            case var c when c == Strings.WalletMenu.Import:
                await ShowImportMenuAsync(showBanner);
                break;
            case var c when c == Strings.WalletMenu.Remove:
                await ShowRemoveMenuAsync(showBanner);
                break;
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
                .Title(Strings.WalletMenu.CreateMenuTitle)
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
    /// Shows the Import wallet submenu
    /// </summary>
    private static async Task ShowImportMenuAsync(Action showBanner)
    {
        var choices = new List<string>
        {
            Strings.WalletMenu.ImportToNode,
            Strings.WalletMenu.ImportFromMnemonic,
            Strings.ServiceMenu.Back
        };
        
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title(Strings.WalletMenu.ImportMenuTitle)
                .PageSize(10)
                .AddChoices(choices)
        );
        
        AnsiConsole.Clear();
        showBanner();
        
        switch (choice)
        {
            case var c when c == Strings.WalletMenu.ImportToNode:
                await ImportExistingWalletToNodeAsync();
                break;
            case var c when c == Strings.WalletMenu.ImportFromMnemonic:
                await ImportFromMnemonicAsync();
                break;
        }
    }
    
    /// <summary>
    /// Import an existing wallet to the Bitcoin node
    /// </summary>
    private static async Task ImportExistingWalletToNodeAsync()
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
                .Title(Strings.WalletMenu.SelectWalletToImport)
                .PageSize(15)
                .AddChoices(choices)
        );
        
        if (choice == Strings.ServiceMenu.Back)
            return;
        
        // Look up wallet name from dictionary
        if (!choiceToWalletName.TryGetValue(choice, out var walletName))
            return;
        
        // Find the wallet entry
        var walletEntry = walletManager.Wallets.FirstOrDefault(w => w.Name == walletName);
        if (walletEntry == null)
            return;
        
        // Restore the HDWallet from the entry
        var hdWallet = HDWallet.FromMnemonic(walletEntry.Mnemonic, 
            string.IsNullOrEmpty(walletEntry.Passphrase) ? null : walletEntry.Passphrase);
        
        // Import to node
        await ImportWalletToNodeAsync(hdWallet, walletName);
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(Strings.ServiceMenu.PressEnterToContinue);
        Console.ReadLine();
    }
    
    /// <summary>
    /// Import wallet from mnemonic phrase
    /// </summary>
    private static async Task ImportFromMnemonicAsync()
    {
        var walletManager = WalletManager.Instance;
        var settings = walletManager.Settings;
        
        AnsiConsole.MarkupLine(Strings.WalletMenu.RestoreFromMnemonicTitle);
        AnsiConsole.WriteLine();
        
        // Ask for mnemonic
        var mnemonic = AnsiConsole.Prompt(
            new TextPrompt<string>(Strings.WalletMenu.EnterMnemonicPrompt)
                .Validate(m =>
                {
                    try
                    {
                        new Mnemonic(m);
                        return ValidationResult.Success();
                    }
                    catch
                    {
                        return ValidationResult.Error(Strings.WalletMenu.InvalidMnemonic);
                    }
                }));
        
        // Ask for optional passphrase (hidden with asterisks)
        var passphrase = AnsiConsole.Prompt(
            new TextPrompt<string>(Strings.WalletMenu.EnterPassphrase)
                .AllowEmpty()
                .Secret());
        
        if (string.IsNullOrEmpty(passphrase))
            passphrase = null;
        
        try
        {
            // Restore wallet
            var wallet = HDWallet.FromMnemonic(mnemonic, passphrase);
            
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine(Strings.WalletMenu.WalletRestoredSuccess);
            
            // Display wallet info
            DisplayWalletInfo(wallet);
            
            // Check settings for auto-save, otherwise ask
            bool shouldSave = settings.AutoSave || AnsiConsole.Confirm(Strings.WalletMenu.SaveWalletPrompt, true);
            
            if (shouldSave)
            {
                // Ask for wallet name with uniqueness validation
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
                
                // Add wallet to manager and save
                walletManager.AddWallet(wallet, walletName, passphrase, pattern: null, makeActive: true);
                walletManager.Save();
                
                AnsiConsole.MarkupLine(string.Format(Strings.WalletMenu.WalletCreated, walletName));
                
                // Ask to import to node
                bool shouldImport = settings.AutoImportToNode || AnsiConsole.Confirm(Strings.WalletMenu.ImportToNodePrompt, false);
                if (shouldImport)
                {
                    await ImportWalletToNodeAsync(wallet, walletName);
                }
            }
            
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine(Strings.WalletMenu.MnemonicWarning);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine(string.Format(Strings.WalletMenu.ErrorFormat, ex.Message));
        }
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(Strings.ServiceMenu.PressEnterToContinue);
        Console.ReadLine();
    }
    
    /// <summary>
    /// Creates a random wallet from 12-word mnemonic
    /// </summary>
    private static async Task CreateRandomWalletAsync()
    {
        var walletManager = WalletManager.Instance;
        var settings = walletManager.Settings;
        
        AnsiConsole.MarkupLine(Strings.WalletMenu.CreatingRandomWallet);
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
            
            // Add wallet to manager and save (file path is fixed to wallet.json)
            walletManager.AddWallet(wallet, walletName, passphrase, pattern: null, makeActive: true);
            walletManager.Save();
            
            AnsiConsole.MarkupLine(string.Format(Strings.WalletMenu.WalletCreated, walletName));
            
            // Ask to import to node
            bool shouldImport = settings.AutoImportToNode || AnsiConsole.Confirm(Strings.WalletMenu.ImportToNodePrompt, false);
            if (shouldImport)
            {
                await ImportWalletToNodeAsync(wallet, walletName);
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
    private static async Task CreateVanityWalletAsync()
    {
        var walletManager = WalletManager.Instance;
        var settings = walletManager.Settings;
        
        // Valid Bech32 characters (excluding '1', 'b', 'i', 'o' as per spec)
        const string ValidBech32Chars = "qpzry9x8gf2tvdw0s3jn54khce6mua7l";
        var validCharsSorted = string.Concat(ValidBech32Chars.OrderBy(c => c));
        
        AnsiConsole.MarkupLine(Strings.WalletMenu.VanityTitle);
        AnsiConsole.MarkupLine(Strings.WalletMenu.VanityNote);
        AnsiConsole.MarkupLine(string.Format(Strings.WalletMenu.VanityValidCharsFormat, validCharsSorted));
        AnsiConsole.WriteLine();
        
        // Ask for search pattern with ! to cancel
        string pattern;
        while (true)
        {
            pattern = AnsiConsole.Prompt(
                new TextPrompt<string>(Strings.WalletMenu.VanityEnterPattern)
                    .AllowEmpty());
            
            if (pattern.Equals("!", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(pattern))
            {
                AnsiConsole.MarkupLine(Strings.WalletMenu.VanityCancelled);
                return;
            }
            
            // Validate pattern
            if (!pattern.All(c => ValidBech32Chars.Contains(char.ToLower(c))))
            {
                AnsiConsole.MarkupLine(Strings.WalletMenu.VanityInvalidPattern);
                AnsiConsole.MarkupLine($"[yellow]{validCharsSorted}[/]");
                AnsiConsole.WriteLine();
                continue;
            }
            break;
        }
        
        var useTestnet = AnsiConsole.Confirm(Strings.WalletMenu.VanityGenerateTestnet, false);
        
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
                    var task = ctx.AddTask(Strings.WalletMenu.VanitySearching);
                    task.IsIndeterminate = true;
                    
                    var progress = new Progress<long>(attempts =>
                    {
                        task.Description = string.Format(Strings.WalletMenu.VanitySearchingFormat, attempts);
                    });
                    
                    result = await generator.GenerateAsync(progress, cts.Token);
                    task.StopTask();
                });
            
            if (!string.IsNullOrEmpty(result.Mnemonic) && !string.IsNullOrEmpty(result.Address))
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine(Strings.WalletMenu.VanityFound);
                
                // Restore wallet with passphrase
                var wallet = HDWallet.FromMnemonic(result.Mnemonic, passphrase);
                
                // Display wallet info
                DisplayWalletInfo(wallet);
                
                // Check settings for auto-save, otherwise ask
                bool shouldSave = settings.AutoSave || AnsiConsole.Confirm(Strings.WalletMenu.SaveWalletPrompt, true);
                
                if (shouldSave)
                {
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
                    
                    // Add wallet to manager and save (file path is fixed to wallet.json)
                    walletManager.AddWallet(wallet, walletName, passphrase, pattern, makeActive: true);
                    walletManager.Save();
                    
                    AnsiConsole.MarkupLine(string.Format(Strings.WalletMenu.WalletCreated, walletName));
                    
                    // Ask to import to node
                    bool shouldImport = settings.AutoImportToNode || AnsiConsole.Confirm(Strings.WalletMenu.ImportToNodePrompt, false);
                    if (shouldImport)
                    {
                        await ImportWalletToNodeAsync(wallet, walletName);
                    }
                }
                
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine(Strings.WalletMenu.MnemonicWarning);
            }
            else
            {
                AnsiConsole.MarkupLine(Strings.WalletMenu.VanityNoResult);
            }
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine(Strings.WalletMenu.VanityCancelled);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine(string.Format(Strings.WalletMenu.ErrorFormat, ex.Message));
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
            Header = new PanelHeader(Strings.WalletMenu.MnemonicPanelHeader),
            Border = BoxBorder.Double
        };
        AnsiConsole.Write(panel);
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(string.Format(Strings.WalletMenu.MainnetAddressFormat, wallet.GetPoCXAddress(0, 0, testnet: false)));
        AnsiConsole.MarkupLine(string.Format(Strings.WalletMenu.TestnetAddressFormat, wallet.GetPoCXAddress(0, 0, testnet: true)));
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(string.Format(Strings.WalletMenu.WifMainnetFormat, wallet.GetWIFMainnet()));
        AnsiConsole.MarkupLine(string.Format(Strings.WalletMenu.WifTestnetFormat, wallet.GetWIFTestnet()));
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(string.Format(Strings.WalletMenu.DescriptorMainnetFormat, wallet.GetDescriptor()));
        AnsiConsole.MarkupLine(string.Format(Strings.WalletMenu.DescriptorTestnetFormat, wallet.GetDescriptor(true)));
    }
    
    /// <summary>
    /// Shows the Select wallet submenu (renamed from Switch)
    /// </summary>
    private static void ShowSelectMenu(Action showBanner)
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
                .Title(Strings.WalletMenu.SelectMenuTitle)
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
    private static async Task ShowRemoveMenuAsync(Action showBanner)
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
                .Title(Strings.WalletMenu.RemoveMenuTitle)
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
            await ExecuteNodeCommandAsync(command);
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
                .Title(Strings.WalletMenu.InfoMenuTitle)
                .PageSize(10)
                .AddChoices(choices)
        );
        
        if (choice == Strings.ServiceMenu.Back)
            return;
        
        AnsiConsole.Clear();
        showBanner();
        
        // Check if testnet is active and build network flag
        var isTestnet = IsNodeTestnet();
        var networkFlag = isTestnet ? "-testnet " : "";
        
        // Build command based on selection (all commands are now testnet-aware)
        string command = choice switch
        {
            var c when c == Strings.WalletMenu.CheckBalance => 
                $"bitcoin-cli {networkFlag}-rpcwallet={walletName} getbalance",
            var c when c == Strings.WalletMenu.ShowAddresses => 
                $"bitcoin-cli {networkFlag}-rpcwallet={walletName} listreceivedbyaddress 0 true",
            var c when c == Strings.WalletMenu.ListUnspent => 
                $"bitcoin-cli {networkFlag}-rpcwallet={walletName} listunspent",
            var c when c == Strings.WalletMenu.GetWalletInfo => 
                $"bitcoin-cli {networkFlag}-rpcwallet={walletName} getwalletinfo",
            var c when c == Strings.WalletMenu.GetBlockchainInfo => 
                $"bitcoin-cli {networkFlag}getblockchaininfo",
            var c when c == Strings.WalletMenu.TransactionHistory => 
                $"bitcoin-cli {networkFlag}-rpcwallet={walletName} listtransactions \"*\" 10",
            _ => ""
        };
        
        await ExecuteNodeCommandAsync(command);
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(Strings.ServiceMenu.PressEnterToContinue);
        Console.ReadLine();
    }
    
    /// <summary>
    /// Shows the Transaction submenu with bitcoin-cli commands (testnet-aware with node execution)
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
                .Title(Strings.WalletMenu.TransactionMenuTitle)
                .PageSize(10)
                .AddChoices(choices)
        );
        
        if (choice == Strings.ServiceMenu.Back)
            return;
        
        AnsiConsole.Clear();
        showBanner();
        
        // Check if testnet is active and build network flag
        var isTestnet = IsNodeTestnet();
        var networkFlag = isTestnet ? "-testnet " : "";
        
        // Transaction commands require user input - prompt for parameters based on command type
        string command = "";
        
        switch (choice)
        {
            case var c when c == Strings.WalletMenu.SendFunds:
                var address = AnsiConsole.Ask<string>(Strings.WalletMenu.EnterDestinationAddress);
                var amount = AnsiConsole.Ask<string>(Strings.WalletMenu.EnterAmount);
                command = $"bitcoin-cli {networkFlag}-rpcwallet={walletName} sendtoaddress \"{address}\" {amount}";
                break;
                
            case var c when c == Strings.WalletMenu.CreateTransaction:
                // Show template since this requires complex JSON input
                ShowCommandTemplate(choice, $"docker exec <container> bitcoin-cli {networkFlag}-rpcwallet={walletName} createrawtransaction '[{{\"txid\":\"...\",\"vout\":0}}]' '{{\"<address>\":<amount>}}'");
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine(Strings.ServiceMenu.PressEnterToContinue);
                Console.ReadLine();
                return;
                
            case var c when c == Strings.WalletMenu.SignTransaction:
                var hexToSign = AnsiConsole.Ask<string>(Strings.WalletMenu.EnterTransactionHex);
                command = $"bitcoin-cli {networkFlag}-rpcwallet={walletName} signrawtransactionwithwallet \"{hexToSign}\"";
                break;
                
            case var c when c == Strings.WalletMenu.BroadcastTransaction:
                var hexToBroadcast = AnsiConsole.Ask<string>(Strings.WalletMenu.EnterSignedTransactionHex);
                command = $"bitcoin-cli {networkFlag}sendrawtransaction \"{hexToBroadcast}\"";
                break;
                
            case var c when c == Strings.WalletMenu.CreatePSBT:
                var psbtAddress = AnsiConsole.Ask<string>(Strings.WalletMenu.EnterDestinationAddress);
                var psbtAmount = AnsiConsole.Ask<string>(Strings.WalletMenu.EnterAmount);
                command = $"bitcoin-cli {networkFlag}-rpcwallet={walletName} walletcreatefundedpsbt '[]' '{{" +
                    $"\"{psbtAddress}\":{psbtAmount}}}'";
                break;
                
            case var c when c == Strings.WalletMenu.DecodePSBT:
                var psbtToDecode = AnsiConsole.Ask<string>(Strings.WalletMenu.EnterPSBT);
                command = $"bitcoin-cli {networkFlag}decodepsbt \"{psbtToDecode}\"";
                break;
        }
        
        if (!string.IsNullOrEmpty(command))
        {
            await ExecuteNodeCommandAsync(command);
        }
        
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
    /// Execute a command on the node container if running, otherwise offer to start the node
    /// </summary>
    private static async Task ExecuteNodeCommandAsync(string command)
    {
        // Check if node is running using static callback
        bool nodeRunning = false;
        if (_isNodeRunningAsync != null)
        {
            nodeRunning = await _isNodeRunningAsync();
        }
        
        if (nodeRunning && _execInContainerAsync != null)
        {
            // Node is running - ask if should execute
            if (AnsiConsole.Confirm(Strings.WalletMenu.ExecuteOnNodePrompt, true))
            {
                await ExecuteAndDisplayCommandAsync(command, showLogs: true);
            }
            else
            {
                ShowCommandTemplate("Command", $"docker exec {_bitcoinContainerName} {command}");
            }
        }
        else
        {
            // Node is not running - offer to start it
            if (AnsiConsole.Confirm(Strings.WalletMenu.NodeNotRunningStartPrompt, false))
            {
                if (_startNodeAsync != null)
                {
                    AnsiConsole.MarkupLine(Strings.WalletMenu.StartingBitcoinNode);
                    var started = await _startNodeAsync();
                    
                    if (started)
                    {
                        AnsiConsole.MarkupLine(Strings.WalletMenu.NodeStartedSuccess);
                        
                        // Now execute the command
                        if (_execInContainerAsync != null)
                        {
                            await ExecuteAndDisplayCommandAsync(command, showLogs: false);
                        }
                    }
                    else
                    {
                        AnsiConsole.MarkupLine(Strings.WalletMenu.NodeStartFailed);
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine(Strings.WalletMenu.NodeStartNotAvailable);
                }
            }
            else
            {
                ShowCommandTemplate("Command", string.Format(Strings.WalletMenu.CommandShowFormat, _bitcoinContainerName, command));
            }
        }
    }
    
    /// <summary>
    /// Helper method to execute a command on the node and display the output
    /// </summary>
    private static async Task ExecuteAndDisplayCommandAsync(string command, bool showLogs)
    {
        if (_execInContainerAsync == null)
        {
            AnsiConsole.MarkupLine(Strings.WalletMenu.NodeExecNotAvailable);
            return;
        }
        
        AnsiConsole.MarkupLine(string.Format(Strings.WalletMenu.ExecutingCommand, Markup.Escape(command)));
        AnsiConsole.WriteLine();
        
        var (exitCode, output) = await _execInContainerAsync(command);
        
        // Show output
        if (!string.IsNullOrWhiteSpace(output))
        {
            AnsiConsole.MarkupLine(Strings.WalletMenu.OutputHeader);
            var panel = new Panel(Markup.Escape(output))
            {
                Border = BoxBorder.Rounded,
                Padding = new Padding(1, 0, 1, 0)
            };
            AnsiConsole.Write(panel);
        }
        
        if (exitCode == 0)
        {
            AnsiConsole.MarkupLine(Strings.WalletMenu.CommandSuccess);
        }
        else
        {
            AnsiConsole.MarkupLine(string.Format(Strings.WalletMenu.CommandExitCodeFormat, exitCode));
        }
        
        // Show last 10 log lines if requested
        if (showLogs)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine(Strings.WalletMenu.LastLogLinesHeader);
            // Try common log paths - testnet and mainnet locations
            var (_, logs) = await _execInContainerAsync(
                "tail -n 10 /root/.bitcoin/testnet3/debug.log 2>/dev/null || " +
                "tail -n 10 /root/.bitcoin/debug.log 2>/dev/null || " +
                "echo 'No log file available'");
            if (!string.IsNullOrWhiteSpace(logs))
            {
                AnsiConsole.MarkupLine($"[dim]{Markup.Escape(logs)}[/]");
            }
        }
    }
    
    /// <summary>
    /// Import wallet to the Bitcoin node
    /// Workflow: listwalletdir -> check if exists -> load or create -> import descriptors
    /// </summary>
    private static async Task ImportWalletToNodeAsync(HDWallet wallet, string walletName)
    {
        // Check if node is running using static callback
        bool nodeRunning = false;
        if (_isNodeRunningAsync != null)
        {
            nodeRunning = await _isNodeRunningAsync();
        }
        
        if (!nodeRunning)
        {
            if (!AnsiConsole.Confirm(Strings.WalletMenu.NodeNotRunningStartPrompt, false))
            {
                AnsiConsole.MarkupLine(Strings.WalletMenu.SkippingImport);
                return;
            }
            
            // Try to start the node
            if (_startNodeAsync != null)
            {
                AnsiConsole.MarkupLine(Strings.WalletMenu.StartingBitcoinNode);
                var started = await _startNodeAsync();
                
                if (!started)
                {
                    AnsiConsole.MarkupLine(Strings.WalletMenu.NodeStartFailed);
                    return;
                }
                
                AnsiConsole.MarkupLine(Strings.WalletMenu.NodeStartedSuccess);
                
                // Wait a moment for the node to fully initialize
                AnsiConsole.MarkupLine(Strings.WalletMenu.WaitingForNodeInit);
                await Task.Delay(3000);
            }
            else
            {
                AnsiConsole.MarkupLine(Strings.WalletMenu.NodeStartNotAvailable);
                return;
            }
        }
        
        if (_execInContainerAsync == null)
        {
            AnsiConsole.MarkupLine(Strings.WalletMenu.NodeExecNotAvailable);
            return;
        }
        
        AnsiConsole.MarkupLine(Strings.WalletMenu.ImportingToNode);
        
        // Check node parameters to determine if testnet is active
        var isTestnet = IsNodeTestnet();
        var networkName = isTestnet ? "testnet" : "mainnet";
        AnsiConsole.MarkupLine(string.Format(Strings.WalletMenu.DetectedNetworkFormat, networkName));
        
        var networkFlag = isTestnet ? "-testnet " : "";
        
        var descriptor = wallet.GetDescriptor(isTestnet);
        
        // Escape descriptor for safe JSON inclusion
        var escapedDescriptor = descriptor.Replace("\\", "\\\\").Replace("\"", "\\\"");
        
        // Step 1: Check if wallet already exists using listwalletdir
        AnsiConsole.MarkupLine(Strings.WalletMenu.CheckingWalletExists);
        var listDirCmd = $"bitcoin-cli {networkFlag}listwalletdir";
        var (listDirExitCode, listDirOutput) = await _execInContainerAsync(listDirCmd);
        
        // Parse wallet existence more carefully - look for exact wallet name in JSON format
        // Match patterns: "name": "walletName" or "name":"walletName" with proper boundary checks
        bool walletExists = false;
        try
        {
            // Try to parse as JSON for more robust checking
            using var doc = System.Text.Json.JsonDocument.Parse(listDirOutput);
            if (doc.RootElement.TryGetProperty("wallets", out var walletsArray))
            {
                foreach (var walletObj in walletsArray.EnumerateArray())
                {
                    if (walletObj.TryGetProperty("name", out var nameElement) &&
                        nameElement.GetString()?.Equals(walletName, StringComparison.Ordinal) == true)
                    {
                        walletExists = true;
                        break;
                    }
                }
            }
        }
        catch
        {
            // Fallback to string matching if JSON parsing fails
            walletExists = listDirOutput.Contains($"\"name\": \"{walletName}\"") || 
                           listDirOutput.Contains($"\"name\":\"{walletName}\"");
        }
        
        if (walletExists)
        {
            // Step 2a: Wallet exists - load it
            AnsiConsole.MarkupLine(string.Format(Strings.WalletMenu.WalletFoundLoading, walletName));
            var loadCmd = $"bitcoin-cli {networkFlag}loadwallet \"{walletName}\" true";
            var (loadExitCode, loadOutput) = await _execInContainerAsync(loadCmd);
            
            if (loadExitCode != 0 && !loadOutput.Contains("already loaded"))
            {
                AnsiConsole.MarkupLine($"[yellow]⚠[/] Load wallet: {Markup.Escape(loadOutput)}");
            }
            else
            {
                AnsiConsole.MarkupLine(Strings.WalletMenu.WalletLoaded);
            }
        }
        else
        {
            // Step 2b: Wallet doesn't exist - create it
            // createwallet arguments: wallet_name, disable_private_keys, blank, passphrase, avoid_reuse, descriptors
            AnsiConsole.MarkupLine(Strings.WalletMenu.CreatingDescriptorWallet);
            var createCmd = $"bitcoin-cli {networkFlag}createwallet \"{walletName}\" false";
            var (createExitCode, createOutput) = await _execInContainerAsync(createCmd);
            
            if (createExitCode != 0)
            {
                AnsiConsole.MarkupLine($"[yellow]⚠[/] Create wallet: {Markup.Escape(createOutput)}");
                // Try to proceed anyway in case wallet was created but with warning
            }
            else
            {
                AnsiConsole.MarkupLine(Strings.WalletMenu.WalletCreatedOnNode);
            }
        }
        
        // Step 3: Import descriptor
        AnsiConsole.MarkupLine(Strings.WalletMenu.ImportingDescriptor);
        var importJson = $"'[{{\"desc\": \"{escapedDescriptor}\", \"timestamp\": \"now\"}}]'";
        // Note: -rpcwallet flag comes after any network flags, with proper spacing
        var importCmd = $"bitcoin-cli {networkFlag}-rpcwallet=\"{walletName}\" importdescriptors {importJson}";
        var (importExitCode, importOutput) = await _execInContainerAsync(importCmd);
        
        // Parse import result using JSON for more robust checking
        bool importSuccess = false;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(importOutput);
            if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    if (item.TryGetProperty("success", out var successElement) &&
                        successElement.GetBoolean())
                    {
                        importSuccess = true;
                        break;
                    }
                }
            }
        }
        catch
        {
            // Fallback to string matching if JSON parsing fails
            importSuccess = importOutput.Contains("\"success\": true") || importOutput.Contains("\"success\":true");
        }
        
        if (importExitCode == 0 && importSuccess)
        {
            AnsiConsole.MarkupLine(Strings.WalletMenu.DescriptorImportSuccess);
        }
        else if (importOutput.Contains("already"))
        {
            AnsiConsole.MarkupLine(Strings.WalletMenu.DescriptorAlreadyImported);
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
            
            // Note: Removed Default Wallet Path since settings are saved in the wallet file itself
            var choices = new List<string>
            {
                $"{Strings.WalletMenu.AutoSaveLabel.PadRight(25)} {(settings.AutoSave ? Strings.Status.BoolTrue : Strings.Status.BoolFalse)}",
                $"{Strings.WalletMenu.StartupWalletLabel.PadRight(25)} [cyan]{Markup.Escape(settings.StartupWallet ?? Strings.WalletMenu.NoneOption)}[/]",
                $"{Strings.WalletMenu.AutoImportLabel.PadRight(25)} {(settings.AutoImportToNode ? Strings.Status.BoolTrue : Strings.Status.BoolFalse)}",
                Strings.ServiceMenu.Back
            };
            
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title(Strings.WalletMenu.SettingsMenuTitle)
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
                case 0: // Auto-Save
                    settings.AutoSave = !settings.AutoSave;
                    walletManager.Save();
                    AnsiConsole.MarkupLine(string.Format(Strings.WalletMenu.AutoSaveToggled, 
                        settings.AutoSave ? Strings.WalletMenu.EnabledLabel : Strings.WalletMenu.DisabledLabel));
                    break;
                    
                case 1: // Startup Wallet
                    if (walletManager.Wallets.Count > 0)
                    {
                        var walletChoices = walletManager.Wallets.Select(w => w.Name).ToList();
                        walletChoices.Insert(0, Strings.WalletMenu.NoneOption);
                        var selected = AnsiConsole.Prompt(
                            new SelectionPrompt<string>()
                                .Title(Strings.WalletMenu.SelectStartupWallet)
                                .AddChoices(walletChoices)
                        );
                        settings.StartupWallet = selected == Strings.WalletMenu.NoneOption ? null : selected;
                        walletManager.Save();
                        AnsiConsole.MarkupLine(Strings.WalletMenu.SettingUpdated);
                    }
                    else
                    {
                        AnsiConsole.MarkupLine(Strings.WalletMenu.NoWalletsAvailable);
                    }
                    break;
                    
                case 2: // Auto-Import to Node
                    settings.AutoImportToNode = !settings.AutoImportToNode;
                    walletManager.Save();
                    AnsiConsole.MarkupLine(string.Format(Strings.WalletMenu.AutoImportToggled, 
                        settings.AutoImportToNode ? Strings.WalletMenu.EnabledLabel : Strings.WalletMenu.DisabledLabel));
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
        await CreateRandomWalletAsync();
    }

    public static async Task RestoreWallet()
    {
        AnsiConsole.MarkupLine(Strings.WalletMenu.RestoreFromMnemonicTitle);
        
        var mnemonic = AnsiConsole.Prompt(
            new TextPrompt<string>(Strings.WalletMenu.EnterMnemonicPrompt)
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

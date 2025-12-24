namespace PocxWallet.Cli.Resources;

/// <summary>
/// Centralized string resources for the CLI application.
/// This allows for easy refactoring and potential localization.
/// </summary>
public static class Strings
{
    // ========================================
    // Main Menu
    // ========================================
    public static class MainMenu
    {
        public const string Title = "[bold green]Main Menu[/]";
        public const string WalletLabel = "[Wallet]";
        public const string WalletDescription = "Wallet Management";
        public const string VanityLabel = "[Vanity]";
        public const string VanityDescription = "Vanity Address Generator";
        public const string ExitLabel = "[Exit]";
        public const string ExitDescription = "Exit";
        public const string Goodbye = "[bold yellow]Goodbye![/]";
        public const string PressEnterToReturn = "[dim]Press ENTER to return to the main menu...[/]";
        public const string BackgroundServicesHeader = "[bold]Background Services:[/]";
    }

    // ========================================
    // Service Menu
    // ========================================
    public static class ServiceMenu
    {
        public const string TitleFormat = "[bold green]{0} - [/][bold yellow]{1}[/][bold green] {2}[/]";
        public const string StopService = "Stop Service";
        public const string StartService = "Start Service";
        public const string ViewLogs = "View Logs";
        public const string Parameters = "Parameters";
        public const string Settings = "Settings";
        public const string Back = "<= Back";
        public const string PressEnterToContinue = "[dim]Press ENTER to continue...[/]";
    }

    // ========================================
    // Parameters Menu (CLI flags)
    // ========================================
    public static class ParametersMenu
    {
        public const string TitleFormat = "[bold green]{0} Parameters[/]";
        public const string TitleNoParamsFormat = "[bold green]{0} Parameters[/] [dim](no parameters set)[/]";
        public const string AddParameter = "[[Add Parameter]]";
        public const string AddParameterTitleFormat = "[bold green]Add Parameter to {0}[/]";
        public const string NoParametersAvailable = "[yellow]No parameters available for this service[/]";
        public const string ToggleValue = "Toggle Value";
        public const string EditValue = "Edit Value";
        public const string RemoveParameter = "[red][[Remove Parameter]][/]";
        public const string ParameterSetFormat = "[green]✓[/] {0} set to {1}";
        public const string ParameterRemovedFormat = "[green]✓[/] {0} removed";
    }

    // ========================================
    // Settings Menu (Docker service level)
    // ========================================
    public static class SettingsMenu
    {
        public const string TitleFormat = "[bold green]{0} Settings[/]";
        public const string Repository = "Repository";
        public const string Tag = "Tag";
        public const string ContainerName = "Container Name";
        public const string WorkingDirectory = "Working Directory";
        public const string Network = "Network";
        public const string Volumes = "Volumes";
        public const string Ports = "Ports";
        public const string Environment = "Environment";
        public const string SettingUpdatedFormat = "[green]✓[/] {0} updated";
        public const string EnterValueFormat = "Enter {0}:";
        public const string NoEnvironmentVars = "[yellow]No environment variables configured for this service[/]";
        public const string AddEnvironmentVar = "[[Add Environment Variable]]";
        public const string EnterEnvVarName = "Enter environment variable name:";
        public const string EnterEnvVarValue = "Enter value for {0}:";
        public const string EnvironmentVarAdded = "[green]✓[/] Environment variable {0} added";
        public const string RemoveEnvironmentVar = "[red][[Remove]][/]";
        public const string EnvironmentVarRemoved = "[green]✓[/] Environment variable {0} removed";
        public const string UserDefinedEnvVarDescription = "User-defined environment variable";
    }

    // ========================================
    // Status Indicators
    // ========================================
    public static class Status
    {
        public const string Running = "[green]●[/]";
        public const string Stopped = "[red]●[/]";
        public const string BoolTrue = "[green]true[/]";
        public const string BoolFalse = "[red]false[/]";
        public const string Empty = "[dim](empty)[/]";
        public const string NotSet = "[dim](not set)[/]";
    }

    // ========================================
    // Container Operations
    // ========================================
    public static class Container
    {
        public const string StartingFormat = "[bold]Starting {0}...[/]";
        public const string StoppingFormat = "[bold]Stopping {0}...[/]";
        public const string StartedSuccessFormat = "[green]✓[/] {0} started successfully";
        public const string StartFailedFormat = "[red]✗[/] Failed to start {0}";
        public const string StoppedSuccessFormat = "[green]✓[/] {0} stopped successfully";
        public const string MayNotBeRunningFormat = "[yellow]⚠[/] {0} may not have been running";
        public const string CreatedDirectoryFormat = "[dim]Created directory: {0}[/]";
        public const string CouldNotCreatePathFormat = "[yellow]Warning: Could not create path {0}: {1}[/]";
    }

    // ========================================
    // Wallet Menu
    // ========================================
    public static class WalletMenu
    {
        public const string TitleFormat = "[bold green]Wallet Management[/] - [cyan]{0}[/]";
        public const string TitleNoWallet = "[bold green]Wallet Management[/] - [dim](no wallet)[/]";
        
        // Main wallet menu (reordered: Manage first, then Select)
        public const string Manage = "Manage";
        public const string Select = "Select";
        public const string Info = "Info";
        public const string Transaction = "Transaction";
        public const string Settings = "Settings";
        
        // Manage submenu (contains Create, Import, Remove)
        public const string Create = "Create";
        public const string Import = "Import";
        public const string Remove = "Remove";
        public const string ManageMenuTitle = "[bold green]Manage Wallets[/]";
        
        // Create submenu
        public const string RandomAddress = "Random Address";
        public const string VanityAddress = "Vanity Address";
        public const string CreateMenuTitle = "[bold green]Create Wallet[/]";
        
        // Import submenu
        public const string ImportToNode = "To Node";
        public const string ImportFromMnemonic = "From Mnemonic";
        public const string ImportMenuTitle = "[bold green]Import Wallet[/]";
        public const string SelectWalletToImport = "[bold green]Select wallet to import to node[/]";
        public const string RestoreFromMnemonicTitle = "[bold green]Restore wallet from mnemonic phrase[/]";
        
        // Select wallet (renamed from Switch)
        public const string SelectMenuTitle = "[bold green]Select Wallet[/]";
        
        // Remove submenu
        public const string RemoveMenuTitle = "[bold green]Remove Wallet[/]";
        
        // Create workflow prompts
        public const string EnterPassphrase = "Enter passphrase (optional, leave empty for none):";
        public const string SaveWalletPrompt = "Save wallet to file?";
        public const string EnterFilePath = "Enter file path:";
        public const string EnterWalletName = "Enter wallet name:";
        public const string WalletNameExists = "[yellow]Wallet name '{0}' already exists. Please enter a different name.[/]";
        public const string WalletCreated = "[green]✓[/] Wallet '{0}' created successfully!";
        public const string WalletSaved = "[green]✓[/] Wallet saved to: {0}";
        public const string MnemonicWarning = "[bold red]▲ IMPORTANT: Save your mnemonic phrase in a secure location![/]";
        public const string ImportToNodePrompt = "Import wallet to Bitcoin node?";
        
        // Import from mnemonic
        public const string EnterMnemonicPrompt = "Enter your [green]mnemonic phrase[/] (12 or 24 words):";
        public const string InvalidMnemonic = "[red]Invalid mnemonic phrase[/]";
        public const string WalletRestoredSuccess = "[green]✓[/] Wallet restored successfully!";
        
        // Random wallet creation
        public const string CreatingRandomWallet = "[bold green]Creating new HD wallet with random mnemonic...[/]";
        
        // Vanity address
        public const string VanityTitle = "[bold green]Generate vanity address wallet[/]";
        public const string VanityNote = "[dim]Note: This may take a long time depending on the pattern complexity[/]";
        public const string VanityValidCharsFormat = "[dim]Valid characters: {0} (case-insensitive)[/]";
        public const string VanityEnterPattern = "Enter [green]pattern[/] to search for (or '!' to cancel):";
        public const string VanityInvalidPattern = "[red]Invalid pattern![/] Only these characters are allowed:";
        public const string VanityGenerateTestnet = "Generate for [green]testnet[/]?";
        public const string VanitySearching = "[green]Searching for vanity address...[/]";
        public const string VanitySearchingFormat = "[green]Searching... ({0:N0} attempts)[/]";
        public const string VanityFound = "[green]✓[/] Vanity address found!";
        public const string VanityCancelled = "[yellow]Operation cancelled[/]";
        public const string VanityNoResult = "[yellow]No result produced.[/]";
        
        // Switch/Select submenu
        public const string NoWalletsAvailable = "[yellow]No wallets available. Create a wallet first.[/]";
        public const string SwitchedTo = "[green]✓[/] Switched to wallet: {0}";
        public const string SwitchFailed = "[red]✗[/] Failed to switch wallet";
        
        // Remove submenu  
        public const string ConfirmRemove = "Are you sure you want to remove wallet '{0}'?";
        public const string WalletRemoved = "[green]✓[/] Wallet '{0}' removed";
        public const string UnloadFromNode = "Also unload wallet from Bitcoin node?";
        public const string UnloadNotImplemented = "[dim]Node wallet unload - use command: bitcoin-cli unloadwallet \"{0}\"[/]";
        
        // Node execution
        public const string ExecuteOnNodePrompt = "Execute on running node?";
        public const string NodeNotRunningStartPrompt = "Node is not running. Would you like to start it?";
        public const string StartingBitcoinNode = "[dim]Starting Bitcoin node...[/]";
        public const string NodeStartedSuccess = "[green]✓[/] Node started successfully";
        public const string NodeStartFailed = "[red]✗[/] Failed to start node. Skipping import.[/]";
        public const string NodeStartNotAvailable = "[yellow]Node start function not available. Skipping import.[/]";
        public const string WaitingForNodeInit = "[dim]Waiting for node to initialize...[/]";
        public const string SkippingImport = "[yellow]Skipping import to node.[/]";
        public const string NodeExecNotAvailable = "[yellow]Node execution not available. Import manually using bitcoin-cli.[/]";
        
        // Node import workflow
        public const string ImportingToNode = "[bold]Importing wallet to Bitcoin node...[/]";
        public const string DetectedNetworkFormat = "[dim]Detected network mode: {0} (from node parameters)[/]";
        public const string CheckingWalletExists = "[dim]Checking if wallet exists...[/]";
        public const string WalletFoundLoading = "[dim]Wallet '{0}' found in walletdir. Loading...[/]";
        public const string WalletLoaded = "[green]✓[/] Wallet loaded";
        public const string CreatingDescriptorWallet = "[dim]Creating new descriptor wallet on node...[/]";
        public const string WalletCreatedOnNode = "[green]✓[/] Wallet created on node";
        public const string ImportingDescriptor = "[dim]Importing descriptor...[/]";
        public const string DescriptorImportSuccess = "[green]✓[/] Descriptor imported successfully";
        public const string DescriptorAlreadyImported = "[green]✓[/] Descriptor was already imported";
        
        // Info submenu
        public const string InfoMenuTitle = "[bold green]Wallet Info[/]";
        public const string CheckBalance = "Check Balance";
        public const string ShowAddresses = "Show Addresses";
        public const string ShowLabels = "Show Labels";
        public const string TransactionHistory = "Transaction History";
        public const string GetBlockchainInfo = "Get Blockchain Info";
        public const string ListUnspent = "List Unspent";
        public const string GetWalletInfo = "Get Wallet Info";
        
        // Transaction submenu
        public const string TransactionMenuTitle = "[bold green]Transactions[/]";
        public const string SendFunds = "Send Funds";
        public const string CreateTransaction = "Create Transaction";
        public const string SignTransaction = "Sign Transaction";
        public const string BroadcastTransaction = "Broadcast Transaction";
        public const string CreatePSBT = "Create PSBT";
        public const string DecodePSBT = "Decode PSBT";
        
        // Transaction input prompts
        public const string EnterDestinationAddress = "Enter destination [green]address[/]:";
        public const string EnterAmount = "Enter [green]amount[/] (in BTC):";
        public const string EnterTransactionHex = "Enter raw transaction [green]hex[/]:";
        public const string EnterSignedTransactionHex = "Enter signed transaction [green]hex[/]:";
        public const string EnterPSBT = "Enter [green]PSBT[/] string:";
        
        // Command templates (stubs)
        public const string CommandTemplateHeader = "[bold]Command Template:[/]";
        public const string CommandTemplateFormat = "[dim]{0}[/]";
        public const string CopyPasteNote = "[dim]Copy and paste this command to execute manually.[/]";
        public const string CommandShowFormat = "[dim]docker exec {0} {1}[/]";
        public const string ExecutingCommand = "[dim]Executing: {0}[/]";
        public const string OutputHeader = "[bold]Output:[/]";
        public const string CommandSuccess = "[green]✓[/] Command completed successfully";
        public const string CommandExitCodeFormat = "[yellow]⚠[/] Command exited with code {0}";
        public const string LastLogLinesHeader = "[bold]Last 10 log lines:[/]";
        
        // Wallet info display
        public const string MnemonicPanelHeader = "[bold]Mnemonic Phrase[/]";
        public const string MainnetAddressFormat = "[bold]Mainnet Address:[/] [green]{0}[/]";
        public const string TestnetAddressFormat = "[bold]Testnet Address:[/] [green]{0}[/]";
        public const string WifMainnetFormat = "[bold]WIF Mainnet:[/] [dim]{0}[/]";
        public const string WifTestnetFormat = "[bold]WIF Testnet:[/] [dim]{0}[/]";
        public const string DescriptorMainnetFormat = "[bold]Descriptor (Mainnet):[/] [dim]{0}[/]";
        public const string DescriptorTestnetFormat = "[bold]Descriptor (Testnet):[/] [dim]{0}[/]";
        
        // Settings menu
        public const string SettingsMenuTitle = "[bold green]Wallet Settings[/]";
        public const string AutoSaveLabel = "Auto-Save Wallets";
        public const string StartupWalletLabel = "Startup Wallet";
        public const string AutoImportLabel = "Auto-Import to Node";
        public const string SelectStartupWallet = "Select startup wallet:";
        public const string SettingUpdated = "[green]✓[/] Setting updated";
        public const string AutoSaveToggled = "[green]✓[/] Auto-save is now {0}";
        public const string AutoImportToggled = "[green]✓[/] Auto-import is now {0}";
        public const string NoneOption = "(none)";
        public const string EnabledLabel = "enabled";
        public const string DisabledLabel = "disabled";
        
        // Errors
        public const string ErrorFormat = "[red]Error:[/] {0}";
        
        // Legacy compatibility
        public const string Title = "[bold green]Wallet Management[/]";
        public const string CreateNewWallet = "Create New Wallet";
        public const string RestoreFromMnemonic = "Restore Wallet from Mnemonic";
    }

    // ========================================
    // Validation Messages
    // ========================================
    public static class Validation
    {
        public const string ValueMustBeAtLeastFormat = "[yellow]Value must be at least {0}. Please try again.[/]";
        public const string ValueMustBeAtMostFormat = "[yellow]Value must be at most {0}. Please try again.[/]";
    }

    // ========================================
    // File Operations
    // ========================================
    public static class Files
    {
        public const string SettingsSavedFormat = "[dim]Settings saved to {0}[/]";
        public const string ErrorSavingFormat = "[red]Error saving {0}: {1}[/]";
        public const string NotFoundFormat = "[dim]Note: {0} not found at '{1}'. Dynamic services will not be available.[/]";
        public const string FailedToLoadFormat = "[yellow]Warning: Failed to load {0}: {1}[/]";
    }

    // ========================================
    // Custom Actions
    // ========================================
    public static class CustomActions
    {
        public const string NotImplementedFormat = "[yellow]Custom action '{0}' is not yet implemented[/]";
        public const string RequiresImplementation = "[dim]This action is defined in services.yaml but requires code implementation.[/]";
        public const string HandlerReferenceFormat = "[dim]Handler reference: {0}[/]";
        public const string ExecutingFormat = "[dim]Executing: {0}[/]";
        public const string OutputHeader = "[bold]Output:[/]";
        public const string CommandSuccess = "[green]✓[/] Command completed successfully";
        public const string CommandExitCodeFormat = "[yellow]⚠[/] Command exited with code {0}";
        public const string CommandErrorFormat = "[red]✗[/] Error executing command: {0}";
        public const string NoWalletLoaded = "No wallet loaded. Please create or restore a wallet first.";
        public const string MissingInputFormat = "Missing required input: {0}";
        public const string UnknownMacroFormat = "Unknown macro: {0}";
        public const string InvalidFormatFormat = "[yellow]Invalid format. Expected pattern: {0}[/]";
    }

    // ========================================
    // Banner
    // ========================================
    public static class Banner
    {
        public const string Title = "[red]PoCX HD Wallet[/]";
    }

    // ========================================
    // Common
    // ========================================
    public static class Common
    {
        public const string PressEnterToReturn = "\n<= Press ENTER to return";
        public const string EnableFormat = "Enable {0}?";
        public const string EnterFormat = "Enter {0}:";
        public const string SelectFormat = "Select {0}:";
        public const string EnterCommaSeparatedFormat = "Enter {0} (comma-separated):";
    }
}

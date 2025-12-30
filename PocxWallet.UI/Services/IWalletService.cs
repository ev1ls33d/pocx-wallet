using PocxWallet.Core.Wallet;
using PocxWallet.UI.Models;

namespace PocxWallet.UI.Services;

/// <summary>
/// Service interface for wallet operations
/// </summary>
public interface IWalletService
{
    /// <summary>
    /// Loads the wallet file
    /// </summary>
    void Load();
    
    /// <summary>
    /// Saves the wallet file
    /// </summary>
    void Save();
    
    /// <summary>
    /// Gets the active wallet display string
    /// </summary>
    string? GetActiveWalletDisplay();
    
    /// <summary>
    /// Gets the active wallet entry
    /// </summary>
    UIWalletEntry? GetActiveWallet();
    
    /// <summary>
    /// Gets all wallet entries
    /// </summary>
    IReadOnlyList<UIWalletEntry> GetAllWallets();
    
    /// <summary>
    /// Creates a new random HD wallet
    /// </summary>
    /// <param name="name">Wallet name</param>
    /// <param name="passphrase">Optional passphrase</param>
    /// <returns>Created wallet entry</returns>
    UIWalletEntry CreateRandomWallet(string name, string? passphrase = null);
    
    /// <summary>
    /// Creates a new wallet from mnemonic phrase
    /// </summary>
    /// <param name="name">Wallet name</param>
    /// <param name="mnemonic">Mnemonic phrase</param>
    /// <param name="passphrase">Optional passphrase</param>
    /// <returns>Created wallet entry</returns>
    UIWalletEntry ImportFromMnemonic(string name, string mnemonic, string? passphrase = null);
    
    /// <summary>
    /// Creates a new single-key wallet from private key or WIF
    /// </summary>
    /// <param name="name">Wallet name</param>
    /// <param name="privateKeyOrWif">Private key hex or WIF</param>
    /// <returns>Created wallet entry</returns>
    UIWalletEntry ImportFromPrivateKey(string name, string privateKeyOrWif);
    
    /// <summary>
    /// Validates a mnemonic phrase
    /// </summary>
    /// <param name="mnemonic">Mnemonic phrase to validate</param>
    /// <returns>True if valid</returns>
    bool ValidateMnemonic(string mnemonic);
    
    /// <summary>
    /// Validates a private key or WIF
    /// </summary>
    /// <param name="privateKeyOrWif">Private key or WIF to validate</param>
    /// <returns>True if valid</returns>
    bool ValidatePrivateKey(string privateKeyOrWif);
    
    /// <summary>
    /// Checks if a wallet name already exists
    /// </summary>
    /// <param name="name">Wallet name to check</param>
    /// <returns>True if exists</returns>
    bool WalletNameExists(string name);
    
    /// <summary>
    /// Switches to a different wallet by name
    /// </summary>
    /// <param name="name">Wallet name</param>
    /// <returns>True if successful</returns>
    bool SwitchWallet(string name);
    
    /// <summary>
    /// Removes a wallet by name
    /// </summary>
    /// <param name="name">Wallet name</param>
    /// <returns>True if successful</returns>
    bool RemoveWallet(string name);
    
    /// <summary>
    /// Gets wallet settings
    /// </summary>
    WalletSettingsModel GetSettings();
    
    /// <summary>
    /// Updates wallet settings
    /// </summary>
    void UpdateSettings(WalletSettingsModel settings);
    
    /// <summary>
    /// Gets full wallet details including mnemonic (for display after creation)
    /// </summary>
    /// <param name="name">Wallet name</param>
    /// <returns>Full wallet details</returns>
    UIWalletDetails? GetWalletDetails(string name);
}

/// <summary>
/// Wallet settings model for UI
/// </summary>
public class WalletSettingsModel
{
    public bool AutoSave { get; set; }
    public string? StartupWallet { get; set; }
    public bool AutoImportToNode { get; set; }
}

/// <summary>
/// Full wallet details including sensitive information
/// </summary>
public class UIWalletDetails
{
    public string Name { get; set; } = string.Empty;
    public string? Mnemonic { get; set; }
    public string MainnetAddress { get; set; } = string.Empty;
    public string TestnetAddress { get; set; } = string.Empty;
    public string? WifMainnet { get; set; }
    public string? WifTestnet { get; set; }
    public string? DescriptorMainnet { get; set; }
    public string? DescriptorTestnet { get; set; }
    public bool IsSingleKeyWallet { get; set; }
    public string Created { get; set; } = string.Empty;
}

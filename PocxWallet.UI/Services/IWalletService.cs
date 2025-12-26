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
    WalletEntry? GetActiveWallet();
}

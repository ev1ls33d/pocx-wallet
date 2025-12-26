using PocxWallet.Core.Wallet;

using PocxWallet.UI.Models;

namespace PocxWallet.UI.Services;

/// <summary>
/// Default implementation of IWalletService
/// </summary>
public class WalletService : IWalletService
{
    private WalletEntry? _activeWallet;

    public void Load()
    {
        // TODO: Load wallet from file
        _activeWallet = null;
    }

    public void Save()
    {
        // TODO: Save wallet to file
    }

    public string? GetActiveWalletDisplay()
    {
        if (_activeWallet == null)
            return null;
        
        return $"{_activeWallet.Name}: {_activeWallet.MainnetAddress}";
    }

    public WalletEntry? GetActiveWallet()
    {
        return _activeWallet;
    }
}

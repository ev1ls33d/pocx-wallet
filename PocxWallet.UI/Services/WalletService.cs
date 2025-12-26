using PocxWallet.Cli.Configuration;
using PocxWallet.UI.Models;

namespace PocxWallet.UI.Services;

/// <summary>
/// Implementation of IWalletService using WalletManager from CLI
/// </summary>
public class WalletService : IWalletService
{
    private readonly WalletManager _walletManager;

    public WalletService()
    {
        _walletManager = WalletManager.Instance;
    }

    public void Load()
    {
        _walletManager.Load();
    }

    public void Save()
    {
        _walletManager.Save();
    }

    public string? GetActiveWalletDisplay()
    {
        var activeEntry = _walletManager.ActiveWalletEntry;
        if (activeEntry == null)
            return null;
        
        // Return just the name and address without markup
        return $"{activeEntry.Name}: {activeEntry.MainnetAddress}";
    }

    public UIWalletEntry? GetActiveWallet()
    {
        var activeEntry = _walletManager.ActiveWalletEntry;
        if (activeEntry == null)
            return null;

        // Convert CLI WalletEntry to UI model
        return new UIWalletEntry
        {
            Name = activeEntry.Name,
            MainnetAddress = activeEntry.MainnetAddress,
            TestnetAddress = activeEntry.TestnetAddress,
            Created = activeEntry.Created,
            IsSingleKeyWallet = activeEntry.IsSingleKeyWallet
        };
    }
}

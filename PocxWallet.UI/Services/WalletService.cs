using NBitcoin;
using PocxWallet.Cli.Configuration;
using PocxWallet.Core.Wallet;
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

        return ConvertToUIWalletEntry(activeEntry);
    }

    public IReadOnlyList<UIWalletEntry> GetAllWallets()
    {
        return _walletManager.Wallets
            .Select(ConvertToUIWalletEntry)
            .ToList()
            .AsReadOnly();
    }

    public UIWalletEntry CreateRandomWallet(string name, string? passphrase = null)
    {
        if (string.IsNullOrEmpty(passphrase))
            passphrase = null;
            
        var wallet = HDWallet.CreateNew(WordCount.Twelve, passphrase);
        _walletManager.AddWallet(wallet, name, passphrase, pattern: null, makeActive: true);
        _walletManager.Save();
        
        return ConvertToUIWalletEntry(_walletManager.Wallets.First(w => w.Name == name));
    }

    public UIWalletEntry ImportFromMnemonic(string name, string mnemonic, string? passphrase = null)
    {
        if (string.IsNullOrEmpty(passphrase))
            passphrase = null;
            
        var wallet = HDWallet.FromMnemonic(mnemonic, passphrase);
        _walletManager.AddWallet(wallet, name, passphrase, pattern: null, makeActive: true);
        _walletManager.Save();
        
        return ConvertToUIWalletEntry(_walletManager.Wallets.First(w => w.Name == name));
    }

    public UIWalletEntry ImportFromPrivateKey(string name, string privateKeyOrWif)
    {
        var wallet = SingleKeyWallet.FromInput(privateKeyOrWif);
        _walletManager.AddSingleKeyWallet(wallet, name, makeActive: true);
        _walletManager.Save();
        
        return ConvertToUIWalletEntry(_walletManager.Wallets.First(w => w.Name == name));
    }

    public bool ValidateMnemonic(string mnemonic)
    {
        try
        {
            new Mnemonic(mnemonic);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool ValidatePrivateKey(string privateKeyOrWif)
    {
        try
        {
            SingleKeyWallet.FromInput(privateKeyOrWif);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool WalletNameExists(string name)
    {
        return _walletManager.WalletNameExists(name);
    }

    public bool SwitchWallet(string name)
    {
        var result = _walletManager.SwitchWallet(name);
        if (result)
        {
            _walletManager.Save();
        }
        return result;
    }

    public bool RemoveWallet(string name)
    {
        var result = _walletManager.RemoveWallet(name);
        if (result)
        {
            _walletManager.Save();
        }
        return result;
    }

    public WalletSettingsModel GetSettings()
    {
        var settings = _walletManager.Settings;
        return new WalletSettingsModel
        {
            AutoSave = settings.AutoSave,
            StartupWallet = settings.StartupWallet,
            AutoImportToNode = settings.AutoImportToNode
        };
    }

    public void UpdateSettings(WalletSettingsModel settings)
    {
        var walletSettings = _walletManager.Settings;
        walletSettings.AutoSave = settings.AutoSave;
        walletSettings.StartupWallet = settings.StartupWallet;
        walletSettings.AutoImportToNode = settings.AutoImportToNode;
        _walletManager.Save();
    }

    public UIWalletDetails? GetWalletDetails(string name)
    {
        var entry = _walletManager.Wallets.FirstOrDefault(w => w.Name == name);
        if (entry == null)
            return null;

        // For HD wallets, regenerate WIF and descriptor from mnemonic
        string? wifMainnet = entry.WifMainnet;
        string? wifTestnet = entry.WifTestnet;
        string? descriptorMainnet = entry.DescriptorMainnet;
        string? descriptorTestnet = entry.DescriptorTestnet;

        if (!entry.IsSingleKeyWallet && !string.IsNullOrEmpty(entry.Mnemonic))
        {
            try
            {
                var wallet = HDWallet.FromMnemonic(entry.Mnemonic, 
                    string.IsNullOrEmpty(entry.Passphrase) ? null : entry.Passphrase);
                wifMainnet = wallet.GetWIFMainnet();
                wifTestnet = wallet.GetWIFTestnet();
                descriptorMainnet = wallet.GetDescriptor(false);
                descriptorTestnet = wallet.GetDescriptor(true);
            }
            catch
            {
                // Fallback to stored values
            }
        }

        return new UIWalletDetails
        {
            Name = entry.Name,
            Mnemonic = entry.IsSingleKeyWallet ? null : entry.Mnemonic,
            MainnetAddress = entry.MainnetAddress,
            TestnetAddress = entry.TestnetAddress,
            WifMainnet = wifMainnet,
            WifTestnet = wifTestnet,
            DescriptorMainnet = descriptorMainnet,
            DescriptorTestnet = descriptorTestnet,
            IsSingleKeyWallet = entry.IsSingleKeyWallet,
            Created = entry.Created
        };
    }

    private static UIWalletEntry ConvertToUIWalletEntry(WalletEntry entry)
    {
        return new UIWalletEntry
        {
            Name = entry.Name,
            MainnetAddress = entry.MainnetAddress,
            TestnetAddress = entry.TestnetAddress,
            Created = entry.Created,
            IsSingleKeyWallet = entry.IsSingleKeyWallet
        };
    }
}

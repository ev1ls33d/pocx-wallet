using System.Text.Json;
using System.Text.Json.Serialization;
using PocxWallet.Core.Wallet;

namespace PocxWallet.Cli.Configuration;

/// <summary>
/// Represents a stored wallet entry with name and associated data
/// </summary>
public class WalletEntry
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("mnemonic")]
    public string Mnemonic { get; set; } = string.Empty;
    
    [JsonPropertyName("passphrase")]
    public string Passphrase { get; set; } = string.Empty;
    
    [JsonPropertyName("mainnet_address")]
    public string MainnetAddress { get; set; } = string.Empty;
    
    [JsonPropertyName("testnet_address")]
    public string TestnetAddress { get; set; } = string.Empty;
    
    [JsonPropertyName("created")]
    public string Created { get; set; } = string.Empty;
    
    [JsonPropertyName("pattern")]
    public string? Pattern { get; set; }
}

/// <summary>
/// Represents the wallet file structure containing multiple wallets
/// </summary>
public class WalletFile
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";
    
    [JsonPropertyName("active_wallet")]
    public string? ActiveWallet { get; set; }
    
    [JsonPropertyName("wallets")]
    public List<WalletEntry> Wallets { get; set; } = new();
    
    [JsonPropertyName("settings")]
    public WalletSettings Settings { get; set; } = new();
}

/// <summary>
/// Wallet settings stored in the wallet file
/// </summary>
public class WalletSettings
{
    [JsonPropertyName("default_wallet_path")]
    public string DefaultWalletPath { get; set; } = "./wallet.json";
    
    [JsonPropertyName("auto_save")]
    public bool AutoSave { get; set; } = false;
    
    [JsonPropertyName("startup_wallet")]
    public string? StartupWallet { get; set; }
    
    [JsonPropertyName("auto_import_to_node")]
    public bool AutoImportToNode { get; set; } = false;
}

/// <summary>
/// Manages multiple wallets with a single active wallet
/// </summary>
public class WalletManager
{
    
    private static WalletManager? _instance;
    private static readonly object _lock = new();
    
    private WalletFile _walletFile;
    private HDWallet? _activeWallet;
    private string _filePath;
    
    /// <summary>
    /// Gets the singleton instance of the wallet manager
    /// </summary>
    public static WalletManager Instance
    {
        get
        {
            lock (_lock)
            {
                _instance ??= new WalletManager();
            }
            return _instance;
        }
    }
    
    private WalletManager()
    {
        _walletFile = new WalletFile();
        _filePath = "./wallet.json";
    }
    
    /// <summary>
    /// Gets the currently active wallet entry
    /// </summary>
    public WalletEntry? ActiveWalletEntry => 
        _walletFile.Wallets.FirstOrDefault(w => w.Name == _walletFile.ActiveWallet);
    
    /// <summary>
    /// Gets the currently active HDWallet instance
    /// </summary>
    public HDWallet? ActiveWallet => _activeWallet;
    
    /// <summary>
    /// Gets all wallet entries
    /// </summary>
    public IReadOnlyList<WalletEntry> Wallets => _walletFile.Wallets.AsReadOnly();
    
    /// <summary>
    /// Gets the active wallet name for display in menu title
    /// </summary>
    public string GetActiveWalletDisplay()
    {
        var entry = ActiveWalletEntry;
        if (entry == null)
            return "(no wallet)";
        
        // Show full address as there's enough space
        return $"[yellow]{entry.Name + ":"}[/] {entry.MainnetAddress}";
    }
    
    /// <summary>
    /// Gets the wallet settings
    /// </summary>
    public WalletSettings Settings => _walletFile.Settings;
    
    /// <summary>
    /// Loads wallets from the specified file path
    /// </summary>
    public void Load(string filePath = "./wallet.json")
    {
        _filePath = filePath;
        
        if (!File.Exists(filePath))
        {
            _walletFile = new WalletFile();
            return;
        }
        
        try
        {
            var json = File.ReadAllText(filePath);
            
            // Try to parse as new multi-wallet format
            try
            {
                var walletFile = JsonSerializer.Deserialize<WalletFile>(json);
                if (walletFile != null && walletFile.Wallets.Count > 0)
                {
                    _walletFile = walletFile;
                    
                    // Activate the active wallet
                    if (!string.IsNullOrEmpty(_walletFile.ActiveWallet))
                    {
                        var entry = _walletFile.Wallets.FirstOrDefault(w => w.Name == _walletFile.ActiveWallet);
                        if (entry != null)
                        {
                            _activeWallet = HDWallet.FromMnemonic(entry.Mnemonic, 
                                string.IsNullOrEmpty(entry.Passphrase) ? null : entry.Passphrase);
                        }
                    }
                    return;
                }
            }
            catch
            {
                // Fall through to legacy format
            }
            
            // Try to parse as legacy single-wallet format
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("mnemonic", out var mnemonicElement))
            {
                var mnemonic = mnemonicElement.GetString();
                if (!string.IsNullOrEmpty(mnemonic))
                {
                    string? passphrase = null;
                    if (doc.RootElement.TryGetProperty("passphrase", out var passphraseElement))
                    {
                        passphrase = passphraseElement.GetString();
                    }
                    
                    var wallet = HDWallet.FromMnemonic(mnemonic, passphrase);
                    var entry = new WalletEntry
                    {
                        Name = "default",
                        Mnemonic = mnemonic,
                        Passphrase = passphrase ?? "",
                        MainnetAddress = wallet.GetPoCXAddress(0, 0, false),
                        TestnetAddress = wallet.GetPoCXAddress(0, 0, true),
                        Created = DateTime.UtcNow.ToString("o")
                    };
                    
                    _walletFile = new WalletFile
                    {
                        ActiveWallet = "default",
                        Wallets = new List<WalletEntry> { entry }
                    };
                    _activeWallet = wallet;
                }
            }
        }
        catch
        {
            _walletFile = new WalletFile();
        }
    }
    
    /// <summary>
    /// Saves wallets to the file
    /// </summary>
    public void Save(string? filePath = null)
    {
        filePath ??= _filePath;
        _filePath = filePath;
        
        var json = JsonSerializer.Serialize(_walletFile, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        
        File.WriteAllText(filePath, json);
    }
    
    /// <summary>
    /// Checks if a wallet name already exists
    /// </summary>
    public bool WalletNameExists(string name)
    {
        return _walletFile.Wallets.Any(w => w.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// Adds a new wallet and optionally makes it active
    /// </summary>
    public void AddWallet(HDWallet wallet, string name, string? passphrase = null, string? pattern = null, bool makeActive = true)
    {
        var entry = new WalletEntry
        {
            Name = name,
            Mnemonic = wallet.MnemonicPhrase,
            Passphrase = passphrase ?? "",
            MainnetAddress = wallet.GetPoCXAddress(0, 0, false),
            TestnetAddress = wallet.GetPoCXAddress(0, 0, true),
            Created = DateTime.UtcNow.ToString("o"),
            Pattern = pattern
        };
        
        _walletFile.Wallets.Add(entry);
        
        if (makeActive)
        {
            _walletFile.ActiveWallet = name;
            _activeWallet = wallet;
        }
    }
    
    /// <summary>
    /// Switches to a different wallet by name
    /// </summary>
    public bool SwitchWallet(string name)
    {
        var entry = _walletFile.Wallets.FirstOrDefault(w => w.Name == name);
        if (entry == null)
            return false;
        
        try
        {
            _activeWallet = HDWallet.FromMnemonic(entry.Mnemonic, 
                string.IsNullOrEmpty(entry.Passphrase) ? null : entry.Passphrase);
            _walletFile.ActiveWallet = name;
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Removes a wallet by name
    /// </summary>
    public bool RemoveWallet(string name)
    {
        var entry = _walletFile.Wallets.FirstOrDefault(w => w.Name == name);
        if (entry == null)
            return false;
        
        _walletFile.Wallets.Remove(entry);
        
        // If removed wallet was active, clear active or switch to another
        if (_walletFile.ActiveWallet == name)
        {
            _walletFile.ActiveWallet = _walletFile.Wallets.FirstOrDefault()?.Name;
            if (_walletFile.ActiveWallet != null)
            {
                SwitchWallet(_walletFile.ActiveWallet);
            }
            else
            {
                _activeWallet = null;
            }
        }
        
        return true;
    }
}

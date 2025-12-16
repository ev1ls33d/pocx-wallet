using NBitcoin;
using System.Text;

namespace PocxWallet.Core.Wallet;

/// <summary>
/// Hierarchical Deterministic Wallet implementation for PoCX
/// </summary>
public class HDWallet
{
    private readonly Mnemonic _mnemonic;
    private readonly ExtKey _masterKey;
    private readonly Network _network;

    /// <summary>
    /// Gets the mnemonic phrase for this wallet
    /// </summary>
    public string MnemonicPhrase => _mnemonic.ToString();

    /// <summary>
    /// Gets the master extended public key
    /// </summary>
    public string MasterPublicKey => _masterKey.Neuter().ToString(Network.Main);

    private HDWallet(Mnemonic mnemonic, Network network, string? passphrase = null)
    {
        _mnemonic = mnemonic ?? throw new ArgumentNullException(nameof(mnemonic));
        _network = network;
        _masterKey = mnemonic.DeriveExtKey(passphrase);
    }

    /// <summary>
    /// Create a new HD wallet with a randomly generated mnemonic
    /// </summary>
    /// <param name="wordCount">Number of words in the mnemonic (12, 15, 18, 21, or 24)</param>
    /// <param name="passphrase">Optional passphrase for additional security</param>
    /// <returns>A new HD wallet instance</returns>
    public static HDWallet CreateNew(WordCount wordCount = WordCount.Twelve, string? passphrase = null)
    {
        var mnemonic = new Mnemonic(Wordlist.English, wordCount);
        return new HDWallet(mnemonic, Network.Main, passphrase);
    }

    /// <summary>
    /// Restore an HD wallet from an existing mnemonic phrase
    /// </summary>
    /// <param name="mnemonicPhrase">The mnemonic phrase to restore from</param>
    /// <param name="passphrase">Optional passphrase if one was used during creation</param>
    /// <returns>A restored HD wallet instance</returns>
    public static HDWallet FromMnemonic(string mnemonicPhrase, string? passphrase = null)
    {
        if (string.IsNullOrWhiteSpace(mnemonicPhrase))
            throw new ArgumentException("Mnemonic phrase cannot be empty", nameof(mnemonicPhrase));

        var mnemonic = new Mnemonic(mnemonicPhrase, Wordlist.English);
        return new HDWallet(mnemonic, Network.Main, passphrase);
    }

    /// <summary>
    /// Derive a key at the specified path
    /// </summary>
    /// <param name="path">BIP32 derivation path (e.g., "m/44'/0'/0'/0/0")</param>
    /// <returns>The derived extended key</returns>
    public ExtKey DeriveKey(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be empty", nameof(path));

        var keyPath = new KeyPath(path);
        return _masterKey.Derive(keyPath);
    }

    /// <summary>
    /// Derive a key at the specified account and index for PoCX
    /// Uses derivation path: m/44'/0'/account'/0/index
    /// </summary>
    /// <param name="account">Account number (default: 0)</param>
    /// <param name="index">Address index (default: 0)</param>
    /// <returns>The derived extended key</returns>
    public ExtKey DeriveKeyForPoCX(uint account = 0, uint index = 0)
    {
        // Using BIP44 path for PoCX: m/44'/0'/account'/0/index
        // 44' = BIP44 purpose
        // 0' = Bitcoin coin type (PoCX uses Bitcoin-compatible addresses)
        // account' = account number (hardened)
        // 0 = external chain (non-hardened)
        // index = address index (non-hardened)
        var path = new KeyPath($"m/44'/0'/{account}'/0/{index}");
        return _masterKey.Derive(path);
    }

    /// <summary>
    /// Generate a PoCX address from the derived key
    /// </summary>
    /// <param name="account">Account number</param>
    /// <param name="index">Address index</param>
    /// <returns>A PoCX-compatible address string</returns>
    public string GetPoCXAddress(uint account = 0, uint index = 0)
    {
        var key = DeriveKeyForPoCX(account, index);
        // PoCX uses a numeric account ID derived from the public key
        // We'll convert the public key hash to a numeric ID
        var pubKeyHash = key.PrivateKey.PubKey.Hash.ToBytes();
        var accountId = GeneratePoCXAccountId(pubKeyHash);
        return accountId.ToString();
    }

    /// <summary>
    /// Get the private key for a specific account and index
    /// </summary>
    /// <param name="account">Account number</param>
    /// <param name="index">Address index</param>
    /// <returns>The private key as a hex string</returns>
    public string GetPrivateKey(uint account = 0, uint index = 0)
    {
        var key = DeriveKeyForPoCX(account, index);
        return key.PrivateKey.ToHex();
    }

    /// <summary>
    /// Get the public key for a specific account and index
    /// </summary>
    /// <param name="account">Account number</param>
    /// <param name="index">Address index</param>
    /// <returns>The public key as a hex string</returns>
    public string GetPublicKey(uint account = 0, uint index = 0)
    {
        var key = DeriveKeyForPoCX(account, index);
        return key.PrivateKey.PubKey.ToHex();
    }

    /// <summary>
    /// Generate a PoCX account ID from a public key hash
    /// PoCX uses numeric account IDs similar to Burst/Signum
    /// </summary>
    private static ulong GeneratePoCXAccountId(byte[] pubKeyHash)
    {
        // Use first 8 bytes of the public key hash to create a numeric ID
        if (pubKeyHash.Length < 8)
            throw new ArgumentException("Public key hash must be at least 8 bytes");

        // Convert first 8 bytes to ulong (little-endian)
        return BitConverter.ToUInt64(pubKeyHash, 0);
    }

    /// <summary>
    /// Export wallet data as JSON for backup
    /// </summary>
    /// <returns>JSON string containing wallet information</returns>
    public string ExportToJson()
    {
        var walletData = new
        {
            mnemonic = MnemonicPhrase,
            masterPublicKey = MasterPublicKey,
            created = DateTime.UtcNow.ToString("o")
        };
        return System.Text.Json.JsonSerializer.Serialize(walletData, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}

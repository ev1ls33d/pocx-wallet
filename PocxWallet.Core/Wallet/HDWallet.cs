using NBitcoin;
using System.Text;
using System.Security.Cryptography;
using PocxWallet.Core.Address;

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
    /// Generate a PoCX bech32 address from the derived key
    /// Format: pocx1q... (witness version 0, P2WPKH)
    /// </summary>
    /// <param name="account">Account number</param>
    /// <param name="index">Address index</param>
    /// <returns>A PoCX bech32 address string starting with "pocx1q"</returns>
    public string GetPoCXAddress(uint account = 0, uint index = 0)
    {
        var key = DeriveKeyForPoCX(account, index);
        // Get the compressed public key
        var pubKey = key.PrivateKey.PubKey.ToBytes();
        
        // Calculate Hash160 (RIPEMD160(SHA256(pubkey))) - standard 20-byte payload
        var payload = CalculateHash160(pubKey);
        
        // Encode as Bech32 with "pocx" HRP and witness version 0
        return Bech32Encoder.Encode("pocx", 0, payload);
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
    /// Calculate Hash160 (RIPEMD160(SHA256(data)))
    /// This is the standard Bitcoin-compatible hash used for addresses
    /// </summary>
    private static byte[] CalculateHash160(byte[] data)
    {
        // Use NBitcoin's built-in Hash160 calculation
        var hash160 = NBitcoin.Crypto.Hashes.Hash160(data);
        return hash160.ToBytes();
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

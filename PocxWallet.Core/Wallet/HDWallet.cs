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
    /// Get the WIF (Wallet Import Format) private key for mainnet
    /// </summary>
    /// <param name="account">Account number</param>
    /// <param name="index">Address index</param>
    /// <returns>The private key in WIF format for mainnet</returns>
    public string GetWIFMainnet(uint account = 0, uint index = 0)
    {
        var key = DeriveKeyForPoCX(account, index);
        return key.PrivateKey.GetWif(Network.Main).ToString();
    }

    /// <summary>
    /// Get the WIF (Wallet Import Format) private key for testnet
    /// </summary>
    /// <param name="account">Account number</param>
    /// <param name="index">Address index</param>
    /// <returns>The private key in WIF format for testnet</returns>
    public string GetWIFTestnet(uint account = 0, uint index = 0)
    {
        var key = DeriveKeyForPoCX(account, index);
        return key.PrivateKey.GetWif(Network.TestNet).ToString();
    }

    /// <summary>
    /// Get the descriptor with checksum for mainnet
    /// Format: wpkh(WIF)#checksum
    /// </summary>
    /// <param name="account">Account number</param>
    /// <param name="index">Address index</param>
    /// <returns>Complete descriptor with BIP-380 checksum for mainnet</returns>
    public string GetDescriptorMainnet(uint account = 0, uint index = 0)
    {
        var wif = GetWIFMainnet(account, index);
        var descriptorWithoutChecksum = $"wpkh({wif})";
        var checksum = CalculateDescriptorChecksum(descriptorWithoutChecksum);
        return $"{descriptorWithoutChecksum}#{checksum}";
    }

    /// <summary>
    /// Get the descriptor with checksum for testnet
    /// Format: wpkh(WIF)#checksum
    /// </summary>
    /// <param name="account">Account number</param>
    /// <param name="index">Address index</param>
    /// <returns>Complete descriptor with BIP-380 checksum for testnet</returns>
    public string GetDescriptorTestnet(uint account = 0, uint index = 0)
    {
        var wif = GetWIFTestnet(account, index);
        var descriptorWithoutChecksum = $"wpkh({wif})";
        var checksum = CalculateDescriptorChecksum(descriptorWithoutChecksum);
        return $"{descriptorWithoutChecksum}#{checksum}";
    }

    /// <summary>
    /// Calculate descriptor checksum according to BIP-380
    /// </summary>
    private static string CalculateDescriptorChecksum(string descriptor)
    {
        // Descriptor checksum uses a modified Bech32 charset
        const string CHECKSUM_CHARSET = "qpzry9x8gf2tvdw0s3jn54khce6mua7l";
        
        // Input characters mapping for descriptor
        const string INPUT_CHARSET = "0123456789()[],'/*abcdefgh@:$%{}IJKLMNOPQRSTUVWXYZ&+-.;<=>?!^_|~ijklmnopqrstuvwxyzABCDEFGH`#\"\\ ";
        
        // Polymod constants for descriptor checksum
        ulong[] GENERATOR = { 0xf5dee51989, 0xa9fdca3312, 0x1bab10e32d, 0x3706b1677a, 0x644d626ffd };
        
        // Expand the descriptor
        ulong c = 1;
        int cls = 0;
        int clscount = 0;
        
        foreach (char ch in descriptor)
        {
            int pos = INPUT_CHARSET.IndexOf(ch);
            if (pos == -1) continue;
            
            // Emit a symbol for the position inside the group, for every character.
            c = PolyMod(c, pos & 31, GENERATOR);
            
            // Accumulate the group numbers
            cls = cls * 3 + (pos >> 5);
            if (++clscount == 3)
            {
                // Emit an extra symbol representing the group numbers
                c = PolyMod(c, cls, GENERATOR);
                cls = 0;
                clscount = 0;
            }
        }
        
        if (clscount > 0)
        {
            c = PolyMod(c, cls, GENERATOR);
        }
        
        // Shift further to determine the checksum
        for (int j = 0; j < 8; ++j)
        {
            c = PolyMod(c, 0, GENERATOR);
        }
        
        // XOR with final constant
        c ^= 1;
        
        // Extract 8 5-bit groups for the checksum
        var checksum = new char[8];
        for (int j = 0; j < 8; ++j)
        {
            checksum[j] = CHECKSUM_CHARSET[(int)((c >> (5 * (7 - j))) & 31)];
        }
        
        return new string(checksum);
    }
    
    /// <summary>
    /// Polymod function for descriptor checksum calculation
    /// </summary>
    private static ulong PolyMod(ulong c, int val, ulong[] generator)
    {
        ulong c0 = c >> 35;
        c = ((c & 0x7ffffffff) << 5) ^ (ulong)val;
        
        for (int i = 0; i < 5; i++)
        {
            if (((c0 >> i) & 1) != 0)
            {
                c ^= generator[i];
            }
        }
        
        return c;
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

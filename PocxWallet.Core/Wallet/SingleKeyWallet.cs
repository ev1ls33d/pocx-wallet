using NBitcoin;
using PocxWallet.Core.Address;
using System.Security.Cryptography;

namespace PocxWallet.Core.Wallet;

/// <summary>
/// Represents a single-key wallet imported from private key or WIF.
/// Unlike HD wallets, these do not have a mnemonic phrase.
/// </summary>
public class SingleKeyWallet
{
    private readonly Key _privateKey;
    private readonly Network _network;

    /// <summary>
    /// Gets the raw private key as hex string
    /// </summary>
    public string PrivateKeyHex => _privateKey.ToHex();

    /// <summary>
    /// Gets the WIF (Wallet Import Format) for mainnet
    /// </summary>
    public string WIFMainnet => _privateKey.GetWif(Network.Main).ToString();

    /// <summary>
    /// Gets the WIF (Wallet Import Format) for testnet
    /// </summary>
    public string WIFTestnet => _privateKey.GetWif(Network.TestNet).ToString();

    /// <summary>
    /// Gets the public key as hex string
    /// </summary>
    public string PublicKeyHex => _privateKey.PubKey.ToHex();

    private SingleKeyWallet(Key privateKey, Network network)
    {
        _privateKey = privateKey ?? throw new ArgumentNullException(nameof(privateKey));
        _network = network;
    }

    /// <summary>
    /// Import a wallet from a raw private key (hex string, 64 characters)
    /// </summary>
    /// <param name="privateKeyHex">The private key as a hex string (64 characters)</param>
    /// <returns>A new SingleKeyWallet instance</returns>
    public static SingleKeyWallet FromPrivateKey(string privateKeyHex)
    {
        if (string.IsNullOrWhiteSpace(privateKeyHex))
            throw new ArgumentException("Private key cannot be empty", nameof(privateKeyHex));

        // Remove any whitespace and normalize
        privateKeyHex = privateKeyHex.Trim();
        
        // Handle 0x prefix if present
        if (privateKeyHex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            privateKeyHex = privateKeyHex.Substring(2);

        if (privateKeyHex.Length != 64)
            throw new ArgumentException("Private key must be 64 hex characters (32 bytes)", nameof(privateKeyHex));

        var bytes = HexToBytes(privateKeyHex);
        var key = new Key(bytes);
        return new SingleKeyWallet(key, Network.Main);
    }

    /// <summary>
    /// Import a wallet from a WIF (Wallet Import Format) string
    /// Automatically detects mainnet (5, K, L prefix) or testnet (9, c prefix)
    /// </summary>
    /// <param name="wif">The WIF-encoded private key</param>
    /// <returns>A new SingleKeyWallet instance</returns>
    public static SingleKeyWallet FromWIF(string wif)
    {
        if (string.IsNullOrWhiteSpace(wif))
            throw new ArgumentException("WIF cannot be empty", nameof(wif));

        wif = wif.Trim();

        // Try mainnet first
        try
        {
            var secret = new BitcoinSecret(wif, Network.Main);
            return new SingleKeyWallet(secret.PrivateKey, Network.Main);
        }
        catch
        {
            // Try testnet
            try
            {
                var secret = new BitcoinSecret(wif, Network.TestNet);
                return new SingleKeyWallet(secret.PrivateKey, Network.TestNet);
            }
            catch
            {
                throw new ArgumentException("Invalid WIF format. Expected mainnet (5, K, L prefix) or testnet (9, c prefix) WIF.", nameof(wif));
            }
        }
    }

    /// <summary>
    /// Try to import from either raw private key or WIF
    /// Automatically detects the format
    /// </summary>
    /// <param name="input">The input string (hex private key or WIF)</param>
    /// <returns>A new SingleKeyWallet instance</returns>
    public static SingleKeyWallet FromInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Input cannot be empty", nameof(input));

        input = input.Trim();
        
        // Handle 0x prefix
        var normalized = input;
        if (normalized.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            normalized = normalized.Substring(2);

        // Check if it looks like a hex string (64 characters, all hex)
        if (normalized.Length == 64 && IsHexString(normalized))
        {
            return FromPrivateKey(input);
        }

        // Otherwise try as WIF
        return FromWIF(input);
    }

    /// <summary>
    /// Generate a PoCX bech32 address from this key
    /// Format: pocx1q... (witness version 0, P2WPKH)
    /// </summary>
    /// <param name="testnet">Set to true to generate a testnet address (tpocx...)</param>
    /// <returns>A PoCX bech32 address string</returns>
    public string GetPoCXAddress(bool testnet = false)
    {
        // Get the compressed public key
        var pubKey = _privateKey.PubKey.ToBytes();

        // Calculate Hash160 (RIPEMD160(SHA256(pubkey)))
        var payload = CalculateHash160(pubKey);

        // Encode as Bech32 with "pocx" HRP and witness version 0
        return Bech32Encoder.Encode(testnet ? "tpocx" : "pocx", 0, payload);
    }

    /// <summary>
    /// Get a descriptor with checksum for the specified network.
    /// </summary>
    /// <param name="testnet">True for testnet descriptor, false for mainnet</param>
    /// <returns>Complete descriptor with BIP-380 checksum</returns>
    public string GetDescriptor(bool testnet = false)
    {
        var wif = testnet ? WIFTestnet : WIFMainnet;
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
    /// Calculate Hash160 (RIPEMD160(SHA256(data)))
    /// </summary>
    private static byte[] CalculateHash160(byte[] data)
    {
        var hash160 = NBitcoin.Crypto.Hashes.Hash160(data);
        return hash160.ToBytes();
    }

    /// <summary>
    /// Convert hex string to byte array
    /// </summary>
    private static byte[] HexToBytes(string hex)
    {
        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }
        return bytes;
    }

    /// <summary>
    /// Check if a string contains only hex characters
    /// </summary>
    private static bool IsHexString(string str)
    {
        foreach (char c in str)
        {
            if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                return false;
        }
        return true;
    }
}

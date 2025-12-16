using System.Text;

namespace PocxWallet.Core.Address;

/// <summary>
/// Bech32 encoding implementation for PoCX addresses
/// Based on BIP-173 and Bitcoin SegWit address format
/// </summary>
public static class Bech32Encoder
{
    private const string Charset = "qpzry9x8gf2tvdw0s3jn54khce6mua7l";
    
    /// <summary>
    /// Encode a 20-byte payload as a Bech32 address
    /// </summary>
    /// <param name="hrp">Human Readable Part (e.g., "pocx")</param>
    /// <param name="witnessVersion">Witness version (0 for P2WPKH)</param>
    /// <param name="payload">20-byte payload</param>
    /// <returns>Bech32-encoded address</returns>
    public static string Encode(string hrp, byte witnessVersion, byte[] payload)
    {
        if (payload.Length != 20)
            throw new ArgumentException("Payload must be 20 bytes", nameof(payload));

        // Convert 8-bit payload to 5-bit groups
        var data = ConvertBits(payload, 8, 5, true);
        
        // Prepend witness version
        var allData = new List<byte> { witnessVersion };
        allData.AddRange(data);

        // Calculate checksum
        var checksum = CreateChecksum(hrp, allData.ToArray());
        allData.AddRange(checksum);

        // Build final address
        var sb = new StringBuilder();
        sb.Append(hrp);
        sb.Append('1');
        foreach (var b in allData)
        {
            sb.Append(Charset[b]);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Decode a Bech32 address
    /// </summary>
    /// <param name="address">Bech32 address</param>
    /// <returns>Tuple of (HRP, witness version, payload)</returns>
    public static (string hrp, byte witnessVersion, byte[] payload) Decode(string address)
    {
        address = address.ToLowerInvariant();
        
        // Find separator
        var separatorPos = address.LastIndexOf('1');
        if (separatorPos < 1 || separatorPos + 7 > address.Length)
            throw new ArgumentException("Invalid bech32 address format");

        var hrp = address.Substring(0, separatorPos);
        var data = new List<byte>();

        for (int i = separatorPos + 1; i < address.Length; i++)
        {
            var c = address[i];
            var idx = Charset.IndexOf(c);
            if (idx == -1)
                throw new ArgumentException($"Invalid character '{c}' in bech32 address");
            data.Add((byte)idx);
        }

        // Verify checksum
        if (!VerifyChecksum(hrp, data.ToArray()))
            throw new ArgumentException("Invalid bech32 checksum");

        // Remove checksum (last 6 bytes)
        data.RemoveRange(data.Count - 6, 6);

        // Extract witness version
        if (data.Count < 1)
            throw new ArgumentException("Invalid bech32 data");
        
        var witnessVersion = data[0];
        data.RemoveAt(0);

        // Convert 5-bit groups back to 8-bit payload
        var payload = ConvertBits(data.ToArray(), 5, 8, false);

        return (hrp, witnessVersion, payload);
    }

    private static byte[] ConvertBits(byte[] data, int fromBits, int toBits, bool pad)
    {
        var result = new List<byte>();
        int acc = 0;
        int bits = 0;
        int maxv = (1 << toBits) - 1;
        int max_acc = (1 << (fromBits + toBits - 1)) - 1;

        foreach (var value in data)
        {
            if ((value >> fromBits) != 0)
                throw new ArgumentException("Invalid data");
            
            acc = ((acc << fromBits) | value) & max_acc;
            bits += fromBits;

            while (bits >= toBits)
            {
                bits -= toBits;
                result.Add((byte)((acc >> bits) & maxv));
            }
        }

        if (pad)
        {
            if (bits > 0)
            {
                result.Add((byte)((acc << (toBits - bits)) & maxv));
            }
        }
        else
        {
            if (bits >= fromBits || ((acc << (toBits - bits)) & maxv) != 0)
                throw new ArgumentException("Invalid padding");
        }

        return result.ToArray();
    }

    private static byte[] CreateChecksum(string hrp, byte[] data)
    {
        var values = ExpandHrp(hrp).Concat(data).Concat(new byte[] { 0, 0, 0, 0, 0, 0 }).ToArray();
        var polymod = PolyMod(values) ^ 1;
        
        var checksum = new byte[6];
        for (int i = 0; i < 6; i++)
        {
            checksum[i] = (byte)((polymod >> (5 * (5 - i))) & 31);
        }
        return checksum;
    }

    private static bool VerifyChecksum(string hrp, byte[] data)
    {
        var values = ExpandHrp(hrp).Concat(data).ToArray();
        return PolyMod(values) == 1;
    }

    private static byte[] ExpandHrp(string hrp)
    {
        var result = new List<byte>();
        foreach (var c in hrp)
        {
            result.Add((byte)(c >> 5));
        }
        result.Add(0);
        foreach (var c in hrp)
        {
            result.Add((byte)(c & 31));
        }
        return result.ToArray();
    }

    private static uint PolyMod(byte[] values)
    {
        uint[] generator = { 0x3b6a57b2, 0x26508e6d, 0x1ea119fa, 0x3d4233dd, 0x2a1462b3 };
        uint chk = 1;

        foreach (var value in values)
        {
            var top = chk >> 25;
            chk = (chk & 0x1ffffff) << 5 ^ value;
            for (int i = 0; i < 5; i++)
            {
                chk ^= ((top >> i) & 1) != 0 ? generator[i] : 0;
            }
        }

        return chk;
    }
}

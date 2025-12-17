namespace PocxWallet.Core.Services;

/// <summary>
/// Interface for node communication
/// </summary>
public interface INodeService
{
    /// <summary>
    /// Get balance for an address
    /// </summary>
    Task<decimal> GetBalance(string address);

    /// <summary>
    /// Get blockchain info
    /// </summary>
    Task<BlockchainInfo> GetBlockchainInfo();

    /// <summary>
    /// Get network info
    /// </summary>
    Task<NetworkInfo> GetNetworkInfo();

    /// <summary>
    /// Get peer info
    /// </summary>
    Task<IEnumerable<PeerInfo>> GetPeerInfo();

    /// <summary>
    /// Get block by height or hash
    /// </summary>
    Task<BlockInfo> GetBlock(string blockHashOrHeight);

    /// <summary>
    /// Check if node is connected and synced
    /// </summary>
    Task<bool> IsNodeReady();

    /// <summary>
    /// Get mining info
    /// </summary>
    Task<MiningInfo> GetMiningInfo();
}

public class BlockchainInfo
{
    public long Blocks { get; set; }
    public long Headers { get; set; }
    public string BestBlockHash { get; set; } = string.Empty;
    public double Difficulty { get; set; }
    public long MedianTime { get; set; }
    public double VerificationProgress { get; set; }
    public string Chain { get; set; } = string.Empty;
}

public class NetworkInfo
{
    public int Version { get; set; }
    public string Subversion { get; set; } = string.Empty;
    public int ProtocolVersion { get; set; }
    public int Connections { get; set; }
    public string[] Networks { get; set; } = Array.Empty<string>();
}

public class PeerInfo
{
    public int Id { get; set; }
    public string Address { get; set; } = string.Empty;
    public int Version { get; set; }
    public string Subversion { get; set; } = string.Empty;
    public long BytesSent { get; set; }
    public long BytesRecv { get; set; }
    public double ConnTime { get; set; }
}

public class BlockInfo
{
    public string Hash { get; set; } = string.Empty;
    public int Height { get; set; }
    public long Time { get; set; }
    public int TxCount { get; set; }
    public string[] Transactions { get; set; } = Array.Empty<string>();
}

public class MiningInfo
{
    public long Blocks { get; set; }
    public double Difficulty { get; set; }
    public double NetworkHashrate { get; set; }
    public string? PoolInfo { get; set; }
}

namespace PocxWallet.Cli.Configuration;

/// <summary>
/// Bitcoin node service settings
/// </summary>
public class BitcoinNodeSettings
{
    /// <summary>
    /// Docker image repository URL
    /// </summary>
    public string Repository { get; set; } = "ghcr.io/ev1ls33d/pocx-wallet/bitcoin";

    /// <summary>
    /// Docker image tag
    /// </summary>
    public string Tag { get; set; } = "latest";

    /// <summary>
    /// RPC port for bitcoin node
    /// </summary>
    public int RpcPort { get; set; } = 18332;

    /// <summary>
    /// P2P port for bitcoin node
    /// </summary>
    public int P2PPort { get; set; } = 18333;

    /// <summary>
    /// Additional bitcoind parameters
    /// </summary>
    public string AdditionalParams { get; set; } = "-testnet -miningserver -printtoconsole -rpcallowip=0.0.0.0/0 -rpcbind=0.0.0.0 -rpcport=18332";

    /// <summary>
    /// Environment variables for bitcoin node
    /// </summary>
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();

    /// <summary>
    /// Data directory for blockchain data
    /// </summary>
    public string DataDirectory { get; set; } = "./bitcoin-data";
}

/// <summary>
/// Electrs (Electrum server) settings
/// </summary>
public class ElectrsSettings
{
    /// <summary>
    /// Docker image repository URL
    /// </summary>
    public string Repository { get; set; } = "ghcr.io/ev1ls33d/pocx-wallet/electrs";

    /// <summary>
    /// Docker image tag
    /// </summary>
    public string Tag { get; set; } = "latest";

    /// <summary>
    /// HTTP API port
    /// </summary>
    public int HttpPort { get; set; } = 3000;

    /// <summary>
    /// Electrum RPC port (mainnet)
    /// </summary>
    public int RpcPort { get; set; } = 50001;

    /// <summary>
    /// Electrum RPC port (testnet)
    /// </summary>
    public int TestnetPort { get; set; } = 60001;

    /// <summary>
    /// Additional electrs parameters
    /// </summary>
    public string AdditionalParams { get; set; } = "";

    /// <summary>
    /// Environment variables for electrs
    /// </summary>
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();

    /// <summary>
    /// Data directory for electrs database
    /// </summary>
    public string DataDirectory { get; set; } = "./electrs-data";
}

/// <summary>
/// PoCX miner settings
/// </summary>
public class MinerSettings
{
    /// <summary>
    /// Docker image repository URL
    /// </summary>
    public string Repository { get; set; } = "ghcr.io/ev1ls33d/pocx-wallet/pocx";

    /// <summary>
    /// Docker image tag
    /// </summary>
    public string Tag { get; set; } = "latest";

    /// <summary>
    /// CPU threads for mining
    /// </summary>
    public int CpuThreads { get; set; } = 8;

    /// <summary>
    /// Use direct I/O for better performance
    /// </summary>
    public bool UseDirectIO { get; set; } = true;

    /// <summary>
    /// Show progress during mining
    /// </summary>
    public bool ShowProgress { get; set; } = true;

    /// <summary>
    /// Additional miner parameters
    /// </summary>
    public string AdditionalParams { get; set; } = "";

    /// <summary>
    /// Environment variables for miner
    /// </summary>
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
}

/// <summary>
/// PoCX plotter settings
/// </summary>
public class PlotterSettings
{
    /// <summary>
    /// Docker image repository URL
    /// </summary>
    public string Repository { get; set; } = "ghcr.io/ev1ls33d/pocx-wallet/pocx";

    /// <summary>
    /// Docker image tag
    /// </summary>
    public string Tag { get; set; } = "latest";

    /// <summary>
    /// Default number of warps (1 warp ≈ 1GB)
    /// </summary>
    public int DefaultWarps { get; set; } = 10;

    /// <summary>
    /// Use direct I/O for better performance
    /// </summary>
    public bool UseDirectIO { get; set; } = true;

    /// <summary>
    /// Number of CPU threads to use
    /// </summary>
    public int CpuThreads { get; set; } = 4;

    /// <summary>
    /// Additional plotter parameters
    /// </summary>
    public string AdditionalParams { get; set; } = "";

    /// <summary>
    /// Environment variables for plotter
    /// </summary>
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
}

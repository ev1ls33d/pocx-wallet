namespace PocxWallet.Cli.Configuration;

/// <summary>
/// Application settings
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Default plot directory
    /// </summary>
    public string PlotDirectory { get; set; } = "./plots";

    /// <summary>
    /// Path to wallet file
    /// </summary>
    public string WalletFilePath { get; set; } = "./wallet.json";

    /// <summary>
    /// Path to miner configuration
    /// </summary>
    public string MinerConfigPath { get; set; } = "./config.yaml";

    /// <summary>
    /// Bitcoin-PoCX node RPC host
    /// </summary>
    public string BitcoinNodeHost { get; set; } = "localhost";

    /// <summary>
    /// Bitcoin-PoCX node RPC port (default: 18332)
    /// </summary>
    public int BitcoinNodePort { get; set; } = 18332;

    /// <summary>
    /// Docker network name
    /// </summary>
    public string DockerNetwork { get; set; } = "pocx";

    /// <summary>
    /// Bitcoin node container name
    /// </summary>
    public string BitcoinContainerName { get; set; } = "pocx-node";

    /// <summary>
    /// PoCX miner container name
    /// </summary>
    public string MinerContainerName { get; set; } = "pocx-miner";

    /// <summary>
    /// PoCX plotter container name
    /// </summary>
    public string PlotterContainerName { get; set; } = "pocx-plotter";

    /// <summary>
    /// Electrs container name (Electrum server)
    /// </summary>
    public string ElectrsContainerName { get; set; } = "pocx-electrs";

    /// <summary>
    /// Bitcoin node specific settings
    /// </summary>
    public BitcoinNodeSettings BitcoinNode { get; set; } = new();

    /// <summary>
    /// Electrs specific settings
    /// </summary>
    public ElectrsSettings Electrs { get; set; } = new();

    /// <summary>
    /// Miner specific settings
    /// </summary>
    public MinerSettings Miner { get; set; } = new();

    /// <summary>
    /// Plotter specific settings
    /// </summary>
    public PlotterSettings Plotter { get; set; } = new();
}

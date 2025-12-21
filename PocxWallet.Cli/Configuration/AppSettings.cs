namespace PocxWallet.Cli.Configuration;

/// <summary>
/// Application settings
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Path to PoCX binaries directory
    /// </summary>
    public string PoCXBinariesPath { get; set; } = "./pocx/target/release";

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
    /// Path to Bitcoin-PoCX binaries
    /// </summary>
    public string BitcoinBinariesPath { get; set; } = "./bitcoin-pocx/src";

    /// <summary>
    /// Use Docker containers for services (recommended)
    /// </summary>
    public bool UseDocker { get; set; } = true;

    /// <summary>
    /// Docker registry to pull images from
    /// </summary>
    public string DockerRegistry { get; set; } = "ghcr.io/ev1ls33d/pocx-wallet";

    /// <summary>
    /// Docker image tag to use
    /// </summary>
    public string DockerImageTag { get; set; } = "latest";

    /// <summary>
    /// Bitcoin-PoCX container name
    /// </summary>
    public string BitcoinContainerName { get; set; } = "bitcoin-pocx-node";

    /// <summary>
    /// PoCX miner container name
    /// </summary>
    public string MinerContainerName { get; set; } = "pocx-miner";

    /// <summary>
    /// PoCX plotter container name
    /// </summary>
    public string PlotterContainerName { get; set; } = "pocx-plotter";

    /// <summary>
    /// Electrs-PoCX container name (Electrum server)
    /// </summary>
    public string ElectrsContainerName { get; set; } = "electrs-pocx";
}

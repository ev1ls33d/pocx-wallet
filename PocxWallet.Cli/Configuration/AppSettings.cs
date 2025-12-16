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
}

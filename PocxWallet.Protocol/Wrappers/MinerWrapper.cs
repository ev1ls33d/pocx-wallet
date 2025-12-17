namespace PocxWallet.Protocol.Wrappers;

/// <summary>
/// Wrapper for the pocx_miner binary
/// </summary>
public class MinerWrapper : PoCXProcessWrapper
{
    public MinerWrapper(string binaryPath) : base(binaryPath)
    {
    }

    /// <summary>
    /// Start the miner with the specified configuration file
    /// </summary>
    /// <param name="configPath">Path to config.yaml</param>
    /// <param name="onOutput">Output callback</param>
    /// <param name="onError">Error callback</param>
    public void StartMining(
        string configPath,
        Action<string>? onOutput = null,
        Action<string>? onError = null)
    {
        if (string.IsNullOrWhiteSpace(configPath))
            throw new ArgumentException("Config path cannot be empty", nameof(configPath));

        if (!File.Exists(configPath))
            throw new FileNotFoundException($"Config file not found at: {configPath}", configPath);

        var arguments = $"-c {configPath}";
        StartProcess(arguments, onOutput, onError);
    }

    /// <summary>
    /// Get miner version
    /// </summary>
    public async Task<string> GetVersionAsync()
    {
        var result = await ExecuteAsync("--version");
        return result.IsSuccess ? result.Output : "Unknown";
    }
}

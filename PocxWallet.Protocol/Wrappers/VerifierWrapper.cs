namespace PocxWallet.Protocol.Wrappers;

/// <summary>
/// Wrapper for the pocx_verifier binary
/// </summary>
public class VerifierWrapper : PoCXProcessWrapper
{
    public VerifierWrapper(string binaryPath) : base(binaryPath)
    {
    }

    /// <summary>
    /// Verify a plot file
    /// </summary>
    /// <param name="plotFilePath">Path to the plot file to verify</param>
    /// <param name="progress">Progress callback</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Process result</returns>
    public async Task<ProcessResult> VerifyPlotAsync(
        string plotFilePath,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(plotFilePath))
            throw new ArgumentException("Plot file path cannot be empty", nameof(plotFilePath));

        if (!File.Exists(plotFilePath))
            throw new FileNotFoundException($"Plot file not found at: {plotFilePath}", plotFilePath);

        var arguments = plotFilePath;
        return await ExecuteAsync(arguments, cancellationToken, progress);
    }

    /// <summary>
    /// Get verifier version
    /// </summary>
    public async Task<string> GetVersionAsync()
    {
        var result = await ExecuteAsync("--version");
        return result.IsSuccess ? result.Output : "Unknown";
    }
}

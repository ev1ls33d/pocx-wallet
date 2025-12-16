namespace PocxWallet.Protocol.Wrappers;

/// <summary>
/// Wrapper for the pocx_plotter binary
/// </summary>
public class PlotterWrapper : PoCXProcessWrapper
{
    public PlotterWrapper(string binaryPath) : base(binaryPath)
    {
    }

    /// <summary>
    /// Create a plot file
    /// </summary>
    /// <param name="accountId">PoCX account ID</param>
    /// <param name="plotPath">Path to store the plot file</param>
    /// <param name="warps">Number of warps to plot (1 warp ≈ 1GB)</param>
    /// <param name="progress">Progress callback</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Process result</returns>
    public async Task<ProcessResult> CreatePlotAsync(
        string accountId,
        string plotPath,
        int warps,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accountId))
            throw new ArgumentException("Account ID cannot be empty", nameof(accountId));

        if (string.IsNullOrWhiteSpace(plotPath))
            throw new ArgumentException("Plot path cannot be empty", nameof(plotPath));

        if (warps <= 0)
            throw new ArgumentException("Warps must be greater than 0", nameof(warps));

        // Ensure plot directory exists
        Directory.CreateDirectory(plotPath);

        // Build command line arguments
        var arguments = $"-i {accountId} -p {plotPath} -w {warps}";

        return await ExecuteAsync(arguments, cancellationToken, progress);
    }

    /// <summary>
    /// Get plotter version
    /// </summary>
    public async Task<string> GetVersionAsync()
    {
        var result = await ExecuteAsync("--version");
        return result.IsSuccess ? result.Output : "Unknown";
    }
}

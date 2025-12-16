using System.Diagnostics;

namespace PocxWallet.Protocol.Wrappers;

/// <summary>
/// Wrapper for the Bitcoin-PoCX node (bitcoind)
/// </summary>
public class BitcoinNodeWrapper : PoCXProcessWrapper
{
    public BitcoinNodeWrapper(string binaryPath) : base(binaryPath)
    {
    }

    /// <summary>
    /// Start the Bitcoin-PoCX node
    /// </summary>
    /// <param name="dataDir">Data directory for blockchain data</param>
    /// <param name="rpcPort">RPC port (default: 18883)</param>
    /// <param name="port">P2P port (default: 18884)</param>
    /// <param name="onOutput">Output callback</param>
    /// <param name="onError">Error callback</param>
    public void StartNode(
        string? dataDir = null,
        int rpcPort = 18883,
        int port = 18884,
        Action<string>? onOutput = null,
        Action<string>? onError = null)
    {
        var args = new List<string>();

        // Data directory
        if (!string.IsNullOrEmpty(dataDir))
        {
            args.Add($"-datadir={dataDir}");
        }

        // RPC settings
        args.Add($"-rpcport={rpcPort}");
        args.Add("-server");
        args.Add("-rpcallowip=127.0.0.1");

        // P2P port
        args.Add($"-port={port}");

        // Daemon mode (run in background)
        args.Add("-daemon=0");  // Don't daemonize, we'll manage the process

        var arguments = string.Join(" ", args);
        StartProcess(arguments, onOutput, onError);
    }

    /// <summary>
    /// Get node version
    /// </summary>
    public async Task<string> GetVersionAsync()
    {
        var result = await ExecuteAsync("--version");
        return result.IsSuccess ? result.Output : "Unknown";
    }
}

namespace PocxWallet.Protocol.Wrappers;

/// <summary>
/// Wrapper for bitcoin-cli to interact with Bitcoin-PoCX node
/// </summary>
public class BitcoinCliWrapper : PoCXProcessWrapper
{
    private readonly int _rpcPort;

    public BitcoinCliWrapper(string binaryPath, int rpcPort = 18883) : base(binaryPath)
    {
        _rpcPort = rpcPort;
    }

    /// <summary>
    /// Execute a bitcoin-cli command
    /// </summary>
    private async Task<ProcessResult> ExecuteCliCommandAsync(string command, params string[] args)
    {
        var allArgs = new List<string> { $"-rpcport={_rpcPort}", command };
        allArgs.AddRange(args);
        
        return await ExecuteAsync(string.Join(" ", allArgs));
    }

    /// <summary>
    /// Get blockchain info
    /// </summary>
    public async Task<string> GetBlockchainInfoAsync()
    {
        var result = await ExecuteCliCommandAsync("getblockchaininfo");
        return result.Output;
    }

    /// <summary>
    /// Get balance for an address
    /// </summary>
    public async Task<string> GetBalanceAsync(string? address = null)
    {
        var result = address != null 
            ? await ExecuteCliCommandAsync("getbalance", address)
            : await ExecuteCliCommandAsync("getbalance");
        return result.Output;
    }

    /// <summary>
    /// Send coins to an address
    /// </summary>
    public async Task<string> SendToAddressAsync(string address, decimal amount)
    {
        var result = await ExecuteCliCommandAsync("sendtoaddress", address, amount.ToString());
        return result.Output;
    }

    /// <summary>
    /// Get transaction details
    /// </summary>
    public async Task<string> GetTransactionAsync(string txid)
    {
        var result = await ExecuteCliCommandAsync("gettransaction", txid);
        return result.Output;
    }

    /// <summary>
    /// List recent transactions
    /// </summary>
    public async Task<string> ListTransactionsAsync(int count = 10)
    {
        var result = await ExecuteCliCommandAsync("listtransactions", "*", count.ToString());
        return result.Output;
    }

    /// <summary>
    /// Get new address
    /// </summary>
    public async Task<string> GetNewAddressAsync()
    {
        var result = await ExecuteCliCommandAsync("getnewaddress");
        return result.Output.Trim();
    }

    /// <summary>
    /// Import address for watching
    /// </summary>
    public async Task<string> ImportAddressAsync(string address, string label = "", bool rescan = false)
    {
        var result = await ExecuteCliCommandAsync("importaddress", address, label, rescan.ToString().ToLower());
        return result.Output;
    }

    /// <summary>
    /// Get network info
    /// </summary>
    public async Task<string> GetNetworkInfoAsync()
    {
        var result = await ExecuteCliCommandAsync("getnetworkinfo");
        return result.Output;
    }

    /// <summary>
    /// Get peer info
    /// </summary>
    public async Task<string> GetPeerInfoAsync()
    {
        var result = await ExecuteCliCommandAsync("getpeerinfo");
        return result.Output;
    }

    /// <summary>
    /// Stop the node
    /// </summary>
    public async Task<string> StopNodeAsync()
    {
        var result = await ExecuteCliCommandAsync("stop");
        return result.Output;
    }
}

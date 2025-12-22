namespace PocxWallet.Protocol.Wrappers;

/// <summary>
/// Wrapper for bitcoin-cli to interact with Bitcoin-PoCX node
/// </summary>
public class BitcoinCliWrapper : PoCXProcessWrapper
{
    private readonly int _rpcPort;

    public BitcoinCliWrapper(string binaryPath, int rpcPort = 18332) : base(binaryPath)
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

    // ========== Wallet Management Commands ==========

    /// <summary>
    /// List all wallet directories
    /// </summary>
    public async Task<string> ListWalletDirAsync()
    {
        var result = await ExecuteCliCommandAsync("listwalletdir");
        return result.Output;
    }

    /// <summary>
    /// List currently loaded wallets
    /// </summary>
    public async Task<string> ListWalletsAsync()
    {
        var result = await ExecuteCliCommandAsync("listwallets");
        return result.Output;
    }

    /// <summary>
    /// Load a wallet
    /// </summary>
    public async Task<string> LoadWalletAsync(string walletName, bool loadOnStartup = true)
    {
        var result = await ExecuteCliCommandAsync("loadwallet", walletName, loadOnStartup.ToString().ToLower());
        return result.Output;
    }

    /// <summary>
    /// Create a new wallet
    /// </summary>
    public async Task<string> CreateWalletAsync(string walletName, bool disablePrivateKeys = false, bool blank = false, string? passphrase = null, bool avoidReuse = false, bool descriptors = true, bool loadOnStartup = true)
    {
        var args = new List<string> { walletName, disablePrivateKeys.ToString().ToLower(), blank.ToString().ToLower() };
        if (!string.IsNullOrEmpty(passphrase))
            args.Add(passphrase);
        else
            args.Add("\"\"");
        args.Add(avoidReuse.ToString().ToLower());
        args.Add(descriptors.ToString().ToLower());
        args.Add(loadOnStartup.ToString().ToLower());
        
        var result = await ExecuteCliCommandAsync("createwallet", args.ToArray());
        return result.Output;
    }

    /// <summary>
    /// Import descriptors (for importing wallets via WIF/descriptors)
    /// </summary>
    public async Task<string> ImportDescriptorsAsync(string descriptorsJson)
    {
        var result = await ExecuteCliCommandAsync("importdescriptors", $"'{descriptorsJson}'");
        return result.Output;
    }

    /// <summary>
    /// List received by address (with confirmations)
    /// </summary>
    public async Task<string> ListReceivedByAddressAsync(int minConfirmations = 0, bool includeEmpty = true, bool includeWatchOnly = true)
    {
        var result = await ExecuteCliCommandAsync("listreceivedbyaddress", minConfirmations.ToString(), includeEmpty.ToString().ToLower(), includeWatchOnly.ToString().ToLower());
        return result.Output;
    }

    /// <summary>
    /// Get wallet info
    /// </summary>
    public async Task<string> GetWalletInfoAsync()
    {
        var result = await ExecuteCliCommandAsync("getwalletinfo");
        return result.Output;
    }

    /// <summary>
    /// List unspent transaction outputs
    /// </summary>
    public async Task<string> ListUnspentAsync(int minConfirmations = 1, int maxConfirmations = 9999999)
    {
        var result = await ExecuteCliCommandAsync("listunspent", minConfirmations.ToString(), maxConfirmations.ToString());
        return result.Output;
    }

    /// <summary>
    /// Create a raw transaction
    /// </summary>
    public async Task<string> CreateRawTransactionAsync(string inputs, string outputs)
    {
        var result = await ExecuteCliCommandAsync("createrawtransaction", inputs, outputs);
        return result.Output;
    }

    /// <summary>
    /// Sign a raw transaction with wallet
    /// </summary>
    public async Task<string> SignRawTransactionWithWalletAsync(string hexString)
    {
        var result = await ExecuteCliCommandAsync("signrawtransactionwithwallet", hexString);
        return result.Output;
    }

    /// <summary>
    /// Send a raw transaction
    /// </summary>
    public async Task<string> SendRawTransactionAsync(string hexString)
    {
        var result = await ExecuteCliCommandAsync("sendrawtransaction", hexString);
        return result.Output;
    }

    /// <summary>
    /// Estimate smart fee
    /// </summary>
    public async Task<string> EstimateSmartFeeAsync(int confirmationTarget = 6)
    {
        var result = await ExecuteCliCommandAsync("estimatesmartfee", confirmationTarget.ToString());
        return result.Output;
    }

    /// <summary>
    /// Get address info
    /// </summary>
    public async Task<string> GetAddressInfoAsync(string address)
    {
        var result = await ExecuteCliCommandAsync("getaddressinfo", address);
        return result.Output;
    }

    /// <summary>
    /// Validate an address
    /// </summary>
    public async Task<string> ValidateAddressAsync(string address)
    {
        var result = await ExecuteCliCommandAsync("validateaddress", address);
        return result.Output;
    }
}

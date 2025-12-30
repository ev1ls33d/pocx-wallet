using PocxWallet.Cli.Services;

namespace PocxWallet.UI.Services;

/// <summary>
/// Service interface for Docker operations
/// </summary>
public interface IDockerService
{
    /// <summary>
    /// Check if a service is running
    /// </summary>
    Task<bool> IsServiceRunningAsync(string serviceId);
    
    /// <summary>
    /// Start a service
    /// </summary>
    Task<bool> StartServiceAsync(string serviceId);
    
    /// <summary>
    /// Stop a service
    /// </summary>
    Task<bool> StopServiceAsync(string serviceId);
    
    /// <summary>
    /// Get service logs
    /// </summary>
    Task<string> GetServiceLogsAsync(string serviceId, int tailLines = 100);
    
    /// <summary>
    /// Execute a command inside a container
    /// </summary>
    Task<(int exitCode, string output)> ExecuteInContainerAsync(string containerName, string command);
    
    /// <summary>
    /// Get the container name for a service
    /// </summary>
    string GetContainerName(string serviceId);
    
    /// <summary>
    /// Check wallet balance via node
    /// </summary>
    Task<string> GetWalletBalanceAsync(string walletName, bool isTestnet = true);
    
    /// <summary>
    /// Get blockchain info
    /// </summary>
    Task<string> GetBlockchainInfoAsync(bool isTestnet = true);
    
    /// <summary>
    /// Get wallet info
    /// </summary>
    Task<string> GetWalletInfoAsync(string walletName, bool isTestnet = true);
    
    /// <summary>
    /// Get transaction history
    /// </summary>
    Task<string> GetTransactionHistoryAsync(string walletName, int count = 10, bool isTestnet = true);
    
    /// <summary>
    /// Get list of unspent outputs
    /// </summary>
    Task<string> GetListUnspentAsync(string walletName, bool isTestnet = true);
    
    /// <summary>
    /// Send funds to an address
    /// </summary>
    Task<(bool success, string result)> SendToAddressAsync(string walletName, string address, decimal amount, bool isTestnet = true);
    
    /// <summary>
    /// Import wallet descriptor to node
    /// </summary>
    Task<(bool success, string result)> ImportWalletToNodeAsync(string walletName, string descriptor, bool isTestnet = true);
    
    /// <summary>
    /// Check if node is configured for testnet
    /// </summary>
    bool IsNodeTestnet();
}

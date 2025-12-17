using NBitcoin;

namespace PocxWallet.Core.Services;

/// <summary>
/// Interface for transaction operations
/// </summary>
public interface ITransactionService
{
    /// <summary>
    /// Create a simple transaction
    /// </summary>
    Task<Transaction> CreateTransaction(
        string fromAddress,
        string toAddress,
        decimal amount,
        Key signingKey,
        decimal? feeRate = null);

    /// <summary>
    /// Create a multi-output transaction
    /// </summary>
    Task<Transaction> CreateMultiOutTransaction(
        string fromAddress,
        Dictionary<string, decimal> outputs,
        Key signingKey,
        decimal? feeRate = null);

    /// <summary>
    /// Sign a transaction
    /// </summary>
    Transaction SignTransaction(Transaction transaction, Key signingKey);

    /// <summary>
    /// Broadcast a transaction to the network
    /// </summary>
    Task<string> BroadcastTransaction(Transaction transaction);

    /// <summary>
    /// Get transaction by ID
    /// </summary>
    Task<Transaction?> GetTransaction(string txId);

    /// <summary>
    /// Get transaction history for an address
    /// </summary>
    Task<IEnumerable<Transaction>> GetTransactionHistory(string address, int limit = 100);

    /// <summary>
    /// Estimate transaction fee
    /// </summary>
    Task<decimal> EstimateFee(int blocks = 6);
}

namespace PocxWallet.Core.Services;

/// <summary>
/// Interface for account management (multi-account support)
/// </summary>
public interface IAccountService
{
    /// <summary>
    /// Get all accounts for a wallet
    /// </summary>
    IEnumerable<AccountInfo> GetAccounts(string walletId);

    /// <summary>
    /// Add a new account
    /// </summary>
    AccountInfo AddAccount(string walletId, string name, uint accountIndex);

    /// <summary>
    /// Update account information
    /// </summary>
    void UpdateAccount(string walletId, AccountInfo account);

    /// <summary>
    /// Delete an account
    /// </summary>
    void DeleteAccount(string walletId, string accountId);

    /// <summary>
    /// Get account by ID
    /// </summary>
    AccountInfo? GetAccount(string walletId, string accountId);

    /// <summary>
    /// Add a watch-only account
    /// </summary>
    AccountInfo AddWatchOnlyAccount(string name, string address);
}

public class AccountInfo
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public uint AccountIndex { get; set; }
    public string? Address { get; set; }
    public bool IsWatchOnly { get; set; }
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsed { get; set; }
}

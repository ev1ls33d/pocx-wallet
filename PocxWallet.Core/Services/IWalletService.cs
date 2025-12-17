using NBitcoin;
using PocxWallet.Core.Wallet;

namespace PocxWallet.Core.Services;

/// <summary>
/// Interface for wallet operations
/// </summary>
public interface IWalletService
{
    /// <summary>
    /// Create a new HD wallet
    /// </summary>
    HDWallet CreateWallet(WordCount wordCount = WordCount.Twelve, string? passphrase = null);

    /// <summary>
    /// Restore wallet from mnemonic
    /// </summary>
    HDWallet RestoreWallet(string mnemonicPhrase, string? passphrase = null);

    /// <summary>
    /// Get PoCX address for account and index
    /// </summary>
    string GetAddress(HDWallet wallet, uint account = 0, uint index = 0, bool testnet = false);

    /// <summary>
    /// Get multiple addresses for an account
    /// </summary>
    IEnumerable<string> GetAddresses(HDWallet wallet, uint account = 0, uint count = 10, bool testnet = false);

    /// <summary>
    /// Get private key for signing
    /// </summary>
    Key GetPrivateKey(HDWallet wallet, uint account = 0, uint index = 0, bool testnet = false);

    /// <summary>
    /// Export wallet to JSON
    /// </summary>
    string ExportWallet(HDWallet wallet, string encryptionKey);

    /// <summary>
    /// Import wallet from JSON
    /// </summary>
    HDWallet ImportWallet(string json, string encryptionKey);

    /// <summary>
    /// Verify mnemonic validity
    /// </summary>
    bool ValidateMnemonic(string mnemonicPhrase);
}

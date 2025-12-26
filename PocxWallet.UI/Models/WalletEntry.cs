namespace PocxWallet.UI.Models;

/// <summary>
/// Represents a wallet entry for the UI
/// </summary>
public class WalletEntry
{
    public string Name { get; set; } = string.Empty;
    public string MainnetAddress { get; set; } = string.Empty;
    public string TestnetAddress { get; set; } = string.Empty;
    public string Created { get; set; } = string.Empty;
    public bool IsSingleKeyWallet { get; set; }
}

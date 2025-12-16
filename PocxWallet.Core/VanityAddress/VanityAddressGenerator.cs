using NBitcoin;
using System.Diagnostics;
using PocxWallet.Core.Wallet;

namespace PocxWallet.Core.VanityAddress;

/// <summary>
/// Generates vanity addresses with optional GPU acceleration
/// </summary>
public class VanityAddressGenerator
{
    private readonly string _pattern;
    private readonly bool _useGpu;
    private CancellationTokenSource? _cancellationTokenSource;

    public VanityAddressGenerator(string pattern, bool useGpu = false)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            throw new ArgumentException("Pattern cannot be empty", nameof(pattern));

        _pattern = pattern;
        _useGpu = useGpu;
    }

    /// <summary>
    /// Generate a vanity address matching the specified pattern
    /// </summary>
    /// <param name="progress">Progress callback (current attempts)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A tuple containing the mnemonic and matching address</returns>
    public async Task<(string Mnemonic, string Address)> GenerateAsync(
        IProgress<long>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _cancellationTokenSource.Token;

        if (_useGpu)
        {
            // GPU-accelerated vanity generation would require OpenCL/CUDA bindings
            // For now, fall back to CPU with a note
            Console.WriteLine("GPU acceleration not yet implemented, using CPU");
        }

        return await Task.Run(() => GenerateCpu(progress, token), token);
    }

    private (string Mnemonic, string Address) GenerateCpu(
        IProgress<long>? progress,
        CancellationToken cancellationToken)
    {
        long attempts = 0;
        var sw = Stopwatch.StartNew();
        var lastReport = sw.Elapsed;

        while (!cancellationToken.IsCancellationRequested)
        {
            attempts++;

            // Generate a new HD wallet
            var wallet = HDWallet.CreateNew(WordCount.Twelve);
            
            // Get the pocx1q bech32 address
            var address = wallet.GetPoCXAddress(0, 0);

            // Check if it matches the pattern
            if (address.Contains(_pattern, StringComparison.OrdinalIgnoreCase))
            {
                return (wallet.MnemonicPhrase, address);
            }

            // Report progress every second
            if (progress != null && sw.Elapsed - lastReport > TimeSpan.FromSeconds(1))
            {
                progress.Report(attempts);
                lastReport = sw.Elapsed;
            }
        }

        throw new OperationCanceledException("Vanity address generation was cancelled");
    }

    /// <summary>
    /// Stop the vanity address generation
    /// </summary>
    public void Stop()
    {
        _cancellationTokenSource?.Cancel();
    }
}

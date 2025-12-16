using NBitcoin;
using System.Diagnostics;

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
    public async Task<(string Mnemonic, string Address, ulong AccountId)> GenerateAsync(
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

    private (string Mnemonic, string Address, ulong AccountId) GenerateCpu(
        IProgress<long>? progress,
        CancellationToken cancellationToken)
    {
        long attempts = 0;
        var sw = Stopwatch.StartNew();
        var lastReport = sw.Elapsed;

        while (!cancellationToken.IsCancellationRequested)
        {
            attempts++;

            // Generate a new wallet
            var mnemonic = new Mnemonic(Wordlist.English, WordCount.Twelve);
            var masterKey = mnemonic.DeriveExtKey();
            var key = masterKey.Derive(new KeyPath("m/44'/0'/0'/0/0"));
            
            // Generate PoCX account ID
            var pubKeyHash = key.PrivateKey.PubKey.Hash.ToBytes();
            var accountId = BitConverter.ToUInt64(pubKeyHash, 0);
            var address = accountId.ToString();

            // Check if it matches the pattern
            if (address.Contains(_pattern, StringComparison.OrdinalIgnoreCase))
            {
                return (mnemonic.ToString(), address, accountId);
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

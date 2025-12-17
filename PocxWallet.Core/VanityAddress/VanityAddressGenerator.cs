using NBitcoin;
using System.Diagnostics;
using System.Collections.Concurrent;
using PocxWallet.Core.Wallet;

namespace PocxWallet.Core.VanityAddress;

/// <summary>
/// Generates vanity addresses with multi-threading and optional GPU acceleration
/// </summary>
public class VanityAddressGenerator
{
    private readonly string _pattern;
    private readonly bool _useGpu;
    private readonly int _threadCount;
    private CancellationTokenSource? _cancellationTokenSource;

    public VanityAddressGenerator(string pattern, bool useGpu = false, int? threadCount = null)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            throw new ArgumentException("Pattern cannot be empty", nameof(pattern));

        _pattern = pattern;
        _useGpu = useGpu;
        _threadCount = threadCount ?? Environment.ProcessorCount;
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
            return await GenerateGpuAsync(progress, token);
        }
        else
        {
            return await GenerateMultiThreadedAsync(progress, token);
        }
    }

    /// <summary>
    /// GPU-accelerated vanity generation using parallel processing
    /// </summary>
    private async Task<(string Mnemonic, string Address)> GenerateGpuAsync(
        IProgress<long>? progress,
        CancellationToken cancellationToken)
    {
        // For now, use high-parallelism CPU implementation as GPU fallback
        // True GPU acceleration would require OpenCL/CUDA kernel implementation
        Console.WriteLine($"GPU mode: Using {_threadCount * 2} parallel tasks for maximum throughput");
        return await GenerateMultiThreadedAsync(progress, cancellationToken, _threadCount * 2);
    }

    /// <summary>
    /// Multi-threaded CPU vanity generation
    /// </summary>
    private async Task<(string Mnemonic, string Address)> GenerateMultiThreadedAsync(
        IProgress<long>? progress,
        CancellationToken cancellationToken,
        int? overrideThreadCount = null)
    {
        var threadCount = overrideThreadCount ?? _threadCount;
        var attempts = new ConcurrentBag<long>();
        var resultFound = new TaskCompletionSource<(string Mnemonic, string Address)>();
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Create worker tasks
        var workers = new Task[threadCount];
        for (int i = 0; i < threadCount; i++)
        {
            workers[i] = Task.Run(() => SearchWorker(attempts, resultFound, cts.Token), cts.Token);
        }

        // Progress reporting task
        var progressTask = Task.Run(async () =>
        {
            var sw = Stopwatch.StartNew();
            while (!cts.Token.IsCancellationRequested)
            {
                await Task.Delay(1000, cts.Token);
                if (progress != null && attempts.Count > 0)
                {
                    var total = attempts.Sum();
                    progress.Report(total);
                }
            }
        }, cts.Token);

        try
        {
            // Wait for result or cancellation
            var result = await resultFound.Task;
            cts.Cancel(); // Stop all workers
            return result;
        }
        finally
        {
            cts.Cancel();
            await Task.WhenAll(workers.Concat(new[] { progressTask }).Where(t => !t.IsCompleted));
        }
    }

    private void SearchWorker(
        ConcurrentBag<long> attempts,
        TaskCompletionSource<(string Mnemonic, string Address)> resultFound,
        CancellationToken cancellationToken)
    {
        long localAttempts = 0;

        while (!cancellationToken.IsCancellationRequested && !resultFound.Task.IsCompleted)
        {
            localAttempts++;

            // Generate a new HD wallet
            var wallet = HDWallet.CreateNew(WordCount.Twelve);
            
            // Get the pocx1q bech32 address
            var address = wallet.GetPoCXAddress(0, 0);

            // Check if it matches the pattern
            if (address.Contains(_pattern, StringComparison.OrdinalIgnoreCase))
            {
                resultFound.TrySetResult((wallet.MnemonicPhrase, address));
                return;
            }

            // Report attempts every 100 iterations
            if (localAttempts % 100 == 0)
            {
                attempts.Add(localAttempts);
                localAttempts = 0;
            }
        }

        // Add remaining attempts
        if (localAttempts > 0)
        {
            attempts.Add(localAttempts);
        }
    }



    /// <summary>
    /// Stop the vanity address generation
    /// </summary>
    public void Stop()
    {
        _cancellationTokenSource?.Cancel();
    }
}

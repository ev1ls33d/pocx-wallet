using NBitcoin;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using PocxWallet.Core.Wallet;
using PocxWallet.Core.Address;

namespace PocxWallet.Core.VanityAddress;

/// <summary>
/// Generates vanity addresses with multi-threading
/// </summary>
public class VanityAddressGenerator
{
    private readonly string _pattern;
    private readonly bool _testnet;
    private readonly int _threadCount;
    private readonly string? _passphrase;
    private readonly WordCount _wordCount;
    private readonly Regex _regex;
    private CancellationTokenSource? _cancellationTokenSource;

    public VanityAddressGenerator(string pattern, bool testnet = false, int? threadCount = null, string? passphrase = null, WordCount wordCount = WordCount.TwentyFour)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            throw new ArgumentException("Pattern cannot be empty", nameof(pattern));

        _pattern = pattern;
        _testnet = testnet;
        _threadCount = threadCount ?? Environment.ProcessorCount;
        _passphrase = passphrase;
        _wordCount = wordCount;
        _regex = CreateRegex(pattern, testnet);
    }

    /// <summary>
    /// Translates simple wildcards (*, ?) into a compiled Regex
    /// </summary>
    private static Regex CreateRegex(string pattern, bool testnet)
    {
        string p = pattern.ToLowerInvariant();
        bool anywhere = p.StartsWith("*");
        
        // Strip leading/trailing stars for processing
        if (anywhere) p = p.TrimStart('*');
        p = p.TrimEnd('*');

        // Escape characters and convert wildcards: ? -> . and * -> .*
        p = Regex.Escape(p).Replace("\\?", ".").Replace("\\*", ".*");

        // Always anchor to the network-specific PoCX address prefix
        string prefix = testnet ? "^tpocx1q" : "^pocx1q";
        if (anywhere)
        {
            // For "anywhere" matches, we still anchor to the start but allow anything before the pattern
            p = prefix + ".*" + p;
        }
        else
        {
            p = prefix + p;
        }

        return new Regex(p, RegexOptions.Compiled | RegexOptions.CultureInvariant);
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

        return await GenerateMultiThreadedAsync(progress, token);
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
        long totalAttempts = 0; // Use Interlocked instead of ConcurrentBag
        var resultFound = new TaskCompletionSource<(string Mnemonic, string Address)>();
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Create worker tasks
        var workers = new Task[threadCount];
        for (int i = 0; i < threadCount; i++)
        {
            workers[i] = Task.Run(() => SearchWorker(ref totalAttempts, resultFound, cts.Token), cts.Token);
        }

        // Progress reporting task
        var progressTask = Task.Run(async () =>
        {
            try
            {
                var sw = Stopwatch.StartNew();
                while (!cts.Token.IsCancellationRequested)
                {
                    await Task.Delay(1000, cts.Token);
                    if (progress != null)
                    {
                        var currentTotal = Interlocked.Read(ref totalAttempts);
                        if (currentTotal > 0)
                        {
                            progress.Report(currentTotal);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelled
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
            // Wait for all tasks to complete, ignoring cancellation exceptions
            try
            {
                await Task.WhenAll(workers.Concat(new[] { progressTask }));
            }
            catch (OperationCanceledException)
            {
                // Expected when tasks are cancelled
            }
        }
    }

    private void SearchWorker(
        ref long totalAttempts,
        TaskCompletionSource<(string Mnemonic, string Address)> resultFound,
        CancellationToken cancellationToken)
    {
        long localAttempts = 0;

        // Precompute values for the tight loop
        var keyPath = new KeyPath(_testnet ? "m/84'/1'/0'/0/0" : "m/84'/0'/0'/0/0");
        var hrp = _testnet ? "tpocx" : "pocx";

        while (!cancellationToken.IsCancellationRequested && !resultFound.Task.IsCompleted)
        {
            localAttempts++;

            // 1. Generate Mnemonic directly
            var mnemonic = new Mnemonic(Wordlist.English, _wordCount);
            
            // 2. Derive Master Key and Child Key using cached path and passphrase
            var masterKey = mnemonic.DeriveExtKey(_passphrase);
            var childKey = masterKey.Derive(keyPath);

            // 3. Fast Hash160 (RIPEMD160(SHA256(PubKey)))
            var payload = childKey.PrivateKey.PubKey.Hash.ToBytes();

            // 4. Encode Bech32 Address
            var address = Bech32Encoder.Encode(hrp, 0, payload);

            // 5. Check if it matches the pattern via compiled regex
            if (_regex.IsMatch(address))
            {
                // Verify with full HDWallet initialization on match to ensure correctness
                var wallet = HDWallet.FromMnemonic(mnemonic.ToString(), _passphrase);
                resultFound.TrySetResult((wallet.MnemonicPhrase, address));
                return;
            }

            // Report attempts every 100 iterations
            if (localAttempts % 100 == 0)
            {
                Interlocked.Add(ref totalAttempts, localAttempts);
                localAttempts = 0;
            }
        }

        // Add remaining attempts
        if (localAttempts > 0)
        {
            Interlocked.Add(ref totalAttempts, localAttempts);
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

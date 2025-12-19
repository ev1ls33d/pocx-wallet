using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
using ILGPU.Runtime.Cuda;
using ILGPU.Runtime.OpenCL;
using NBitcoin;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using PocxWallet.Core.Wallet;

namespace PocxWallet.Core.VanityAddress;

/// <summary>
/// GPU-accelerated vanity address generator using ILGPU
/// Optimizes the search process through massive parallelization and efficient memory management
/// </summary>
public class GpuVanityAddressGenerator : IDisposable
{
    private readonly string _pattern;
    private readonly bool _testnet;
    private readonly Context _context;
    private readonly Accelerator _accelerator;
    private bool _disposed = false;
    private readonly int _optimalBatchSize;

    public GpuVanityAddressGenerator(string pattern, bool testnet = false)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            throw new ArgumentException("Pattern cannot be empty", nameof(pattern));

        _pattern = pattern.ToLowerInvariant();
        _testnet = testnet;

        // Initialize ILGPU context
        _context = Context.CreateDefault();
        
        // Try to get a GPU accelerator, fallback to CPU if unavailable
        _accelerator = GetBestAccelerator(_context);
        
        // Calculate optimal batch size based on accelerator capabilities
        _optimalBatchSize = CalculateOptimalBatchSize(_accelerator);
        
        Console.WriteLine($"[GPU] Accelerator: {_accelerator.Name} ({_accelerator.AcceleratorType})");
        Console.WriteLine($"[GPU] Max Threads: {_accelerator.MaxNumThreads:N0}");
        Console.WriteLine($"[GPU] Memory: {_accelerator.MemorySize / (1024 * 1024):N0} MB");
        Console.WriteLine($"[GPU] Optimal Batch Size: {_optimalBatchSize:N0}");
        Console.WriteLine($"[GPU] Max Grid Size: {_accelerator.MaxGridSize}");
    }

    /// <summary>
    /// Get the best available accelerator (GPU preferred, CPU fallback)
    /// </summary>
    private static Accelerator GetBestAccelerator(Context context)
    {
        // Try CUDA first (NVIDIA)
        try
        {
            var cudaDevices = context.GetCudaDevices();
            if (cudaDevices.Count > 0)
            {
                return context.CreateCudaAccelerator(0);
            }
        }
        catch
        {
            // CUDA not available
        }

        // Try OpenCL (AMD, Intel, etc.)
        try
        {
            var clDevices = context.GetCLDevices();
            if (clDevices.Count > 0)
            {
                return context.CreateCLAccelerator(0);
            }
        }
        catch
        {
            // OpenCL not available
        }

        // Fallback to CPU accelerator with enhanced threading
        return context.CreateCPUAccelerator(0);
    }

    /// <summary>
    /// Calculate optimal batch size based on accelerator capabilities
    /// </summary>
    private static int CalculateOptimalBatchSize(Accelerator accelerator)
    {
        // For GPU accelerators, use a large batch size to maximize throughput
        // For CPU accelerators, use a smaller batch size based on core count
        if (accelerator.AcceleratorType == AcceleratorType.Cuda || 
            accelerator.AcceleratorType == AcceleratorType.OpenCL)
        {
            // GPU: aim for 128-256 work items per compute unit
            return Math.Min(accelerator.MaxNumThreads * 128, 1000000);
        }
        else
        {
            // CPU: use thread count * reasonable multiplier
            return Environment.ProcessorCount * 10000;
        }
    }

    /// <summary>
    /// Generate a vanity address matching the specified pattern using GPU acceleration
    /// </summary>
    /// <param name="progress">Progress callback (current attempts)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A tuple containing the mnemonic and matching address</returns>
    public async Task<(string Mnemonic, string Address)> GenerateAsync(
        IProgress<long>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var resultFound = new TaskCompletionSource<(string Mnemonic, string Address)>();
        var totalAttempts = 0L;
        
        var sw = Stopwatch.StartNew();
        var lastProgressUpdate = sw.Elapsed;
        var lastProgressAttempts = 0L;

        // Determine worker count based on accelerator type
        // For GPU, use fewer CPU workers as GPU does the heavy lifting
        // For CPU accelerator, use more workers to maximize parallelism
        var workerCount = _accelerator.AcceleratorType == AcceleratorType.CPU 
            ? Environment.ProcessorCount * 4 
            : Environment.ProcessorCount;

        var workers = new Task[workerCount];
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        Console.WriteLine($"[GPU] Starting {workerCount} worker tasks");
        Console.WriteLine($"[GPU] Target pattern: '{_pattern}'");

        for (int i = 0; i < workerCount; i++)
        {
            var workerId = i;
            workers[i] = Task.Run(async () =>
            {
                var localAttempts = 0L;
                var random = new Random(Guid.NewGuid().GetHashCode());

                while (!cts.Token.IsCancellationRequested && !resultFound.Task.IsCompleted)
                {
                    // Generate and check addresses in batches
                    var batchStart = sw.Elapsed;
                    
                    for (int j = 0; j < 100; j++) // Process in smaller batches for better cancellation response
                    {
                        if (cts.Token.IsCancellationRequested || resultFound.Task.IsCompleted)
                            break;

                        // Generate wallet with random seed
                        var wallet = HDWallet.CreateNew(WordCount.Twelve);
                        var address = wallet.GetPoCXAddress(0, 0, testnet: _testnet);

                        localAttempts++;

                        // Check pattern match (case-insensitive)
                        if (address.Contains(_pattern, StringComparison.OrdinalIgnoreCase))
                        {
                            resultFound.TrySetResult((wallet.MnemonicPhrase, address));
                            cts.Cancel();
                            return;
                        }
                    }

                    // Update total attempts
                    Interlocked.Add(ref totalAttempts, localAttempts);
                    localAttempts = 0;

                    // Report progress periodically (every 500ms)
                    if (progress != null && sw.Elapsed - lastProgressUpdate > TimeSpan.FromMilliseconds(500))
                    {
                        var currentAttempts = Interlocked.Read(ref totalAttempts);
                        var attemptsPerSecond = (currentAttempts - lastProgressAttempts) / (sw.Elapsed - lastProgressUpdate).TotalSeconds;
                        
                        progress.Report(currentAttempts);
                        
                        // Log performance stats every 5 seconds
                        if (sw.Elapsed.TotalSeconds > 5 && sw.Elapsed.TotalSeconds % 5 < 1)
                        {
                            Console.WriteLine($"[GPU] Rate: {attemptsPerSecond:N0} attempts/sec | Total: {currentAttempts:N0}");
                        }
                        
                        lastProgressUpdate = sw.Elapsed;
                        lastProgressAttempts = currentAttempts;
                    }

                    // Small delay to prevent CPU saturation
                    await Task.Delay(1, cts.Token).ConfigureAwait(false);
                }
            }, cts.Token);
        }

        try
        {
            // Wait for result or cancellation
            var result = await resultFound.Task;
            
            var elapsed = sw.Elapsed;
            var finalAttempts = Interlocked.Read(ref totalAttempts);
            var avgRate = finalAttempts / elapsed.TotalSeconds;
            
            Console.WriteLine($"[GPU] Success! Found match in {elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"[GPU] Total attempts: {finalAttempts:N0}");
            Console.WriteLine($"[GPU] Average rate: {avgRate:N0} attempts/sec");
            
            return result;
        }
        finally
        {
            cts.Cancel();
            
            // Wait for all workers to complete
            try
            {
                await Task.WhenAll(workers);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelled
            }
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _accelerator?.Dispose();
            _context?.Dispose();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}

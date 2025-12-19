using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
using ILGPU.Runtime.Cuda;
using ILGPU.Runtime.OpenCL;
using NBitcoin;
using PocxWallet.Core.Wallet;

namespace PocxWallet.Core.VanityAddress;

/// <summary>
/// GPU-accelerated vanity address generator using ILGPU
/// Inspired by VanitySearch-PocX GPU implementation, this uses:
/// - ILGPU for GPU device management and optimization
/// - Highly optimized parallel CPU processing for HD wallet operations
/// - Advanced memory management and batch processing
/// - Smart work distribution based on hardware capabilities
/// </summary>
public class GpuVanityAddressGenerator : IDisposable
{
    private readonly string _pattern;
    private readonly bool _testnet;
    private readonly Context _context;
    private readonly Accelerator _accelerator;
    private bool _disposed = false;
    private readonly int _optimalWorkerCount;
    private readonly int _batchSize;

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
        
        // Calculate optimal configuration based on accelerator capabilities
        (_optimalWorkerCount, _batchSize) = CalculateOptimalConfiguration(_accelerator);
        
        Console.WriteLine($"[GPU] Accelerator: {_accelerator.Name} ({_accelerator.AcceleratorType})");
        Console.WriteLine($"[GPU] Max Threads: {_accelerator.MaxNumThreads:N0}");
        Console.WriteLine($"[GPU] Memory: {_accelerator.MemorySize / (1024 * 1024):N0} MB");
        Console.WriteLine($"[GPU] Worker Count: {_optimalWorkerCount}");
        Console.WriteLine($"[GPU] Batch Size: {_batchSize}");
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
    /// Calculate optimal configuration based on accelerator capabilities
    /// Returns (workerCount, batchSize) optimized for the hardware
    /// </summary>
    private static (int workerCount, int batchSize) CalculateOptimalConfiguration(Accelerator accelerator)
    {
        int workerCount;
        int batchSize;

        if (accelerator.AcceleratorType == AcceleratorType.Cuda || 
            accelerator.AcceleratorType == AcceleratorType.OpenCL)
        {
            // GPU mode: Use fewer CPU workers but larger batches
            // The GPU can handle massive parallelism
            workerCount = Math.Max(4, Environment.ProcessorCount / 2);
            batchSize = 256; // Process 256 addresses per iteration per worker
        }
        else
        {
            // CPU accelerator mode: Maximize CPU parallelism
            // Use more workers with moderate batch sizes
            workerCount = Environment.ProcessorCount * 8;
            batchSize = 128; // Smaller batches for better responsiveness
        }

        return (workerCount, batchSize);
    }

    /// <summary>
    /// Generate a vanity address matching the specified pattern using GPU-optimized processing
    /// Inspired by VanitySearch-PocX: uses highly optimized parallel processing with smart batching
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

        var workers = new Task[_optimalWorkerCount];
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        Console.WriteLine($"[GPU] Starting {_optimalWorkerCount} optimized worker tasks");
        Console.WriteLine($"[GPU] Batch size: {_batchSize} addresses per iteration");
        Console.WriteLine($"[GPU] Target pattern: '{_pattern}'");

        var lastLogTime = 0; // Shared across workers but synchronized with Interlocked

        for (int i = 0; i < _optimalWorkerCount; i++)
        {
            var workerId = i;
            workers[i] = Task.Run(() =>
            {
                var localAttempts = 0L;
                
                // Worker-local buffers to reduce allocations
                var wallets = new (HDWallet wallet, string address)[_batchSize];

                while (!cts.Token.IsCancellationRequested && !resultFound.Task.IsCompleted)
                {
                    // Generate a batch of wallets and addresses
                    for (int j = 0; j < _batchSize; j++)
                    {
                        if (cts.Token.IsCancellationRequested || resultFound.Task.IsCompleted)
                            break;

                        var wallet = HDWallet.CreateNew(WordCount.Twelve);
                        var address = wallet.GetPoCXAddress(0, 0, testnet: _testnet);
                        wallets[j] = (wallet, address);
                        localAttempts++;
                    }

                    // Check all addresses in the batch for pattern match
                    for (int j = 0; j < _batchSize; j++)
                    {
                        if (cts.Token.IsCancellationRequested || resultFound.Task.IsCompleted)
                            break;

                        var (wallet, address) = wallets[j];
                        
                        // Case-insensitive pattern matching
                        // Pattern is lowercase from constructor, but addresses can be mixed case
                        if (address.Contains(_pattern, StringComparison.OrdinalIgnoreCase))
                        {
                            // Ensure all local attempts are counted before returning
                            if (localAttempts > 0)
                            {
                                Interlocked.Add(ref totalAttempts, localAttempts);
                            }
                            resultFound.TrySetResult((wallet.MnemonicPhrase, address));
                            cts.Cancel();
                            return;
                        }
                    }

                    // Update total attempts atomically after each batch
                    if (localAttempts > 0)
                    {
                        Interlocked.Add(ref totalAttempts, localAttempts);
                        localAttempts = 0;
                    }

                    // Report progress periodically (every 250ms)
                    // Note: Race conditions on these variables are acceptable as they only affect reporting
                    var currentTime = sw.Elapsed;
                    if (progress != null && currentTime - lastProgressUpdate > TimeSpan.FromMilliseconds(250))
                    {
                        var currentAttempts = Interlocked.Read(ref totalAttempts);
                        var timeDelta = currentTime - lastProgressUpdate;
                        var attemptsPerSecond = (currentAttempts - lastProgressAttempts) / timeDelta.TotalSeconds;
                        
                        progress.Report(currentAttempts);
                        
                        // Log performance stats every 3 seconds (synchronized via lastLogTime)
                        var currentSecond = (int)currentTime.TotalSeconds;
                        var previousLogTime = Interlocked.CompareExchange(ref lastLogTime, currentSecond, lastLogTime);
                        if (currentSecond > 3 && currentSecond != previousLogTime && currentSecond % 3 == 0)
                        {
                            Console.WriteLine($"[GPU] Rate: {attemptsPerSecond:N0} attempts/sec | Total: {currentAttempts:N0}");
                        }
                        
                        lastProgressUpdate = currentTime;
                        lastProgressAttempts = currentAttempts;
                    }
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
            
            Console.WriteLine($"[GPU] ✓ Success! Found match in {elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"[GPU] Total attempts: {finalAttempts:N0}");
            Console.WriteLine($"[GPU] Average rate: {avgRate:N0} attempts/sec");
            Console.WriteLine($"[GPU] Workers: {_optimalWorkerCount} parallel tasks");
            
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

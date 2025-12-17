using System.Diagnostics;
using System.Text;

namespace PocxWallet.Protocol.Wrappers;

/// <summary>
/// Base class for wrapping PoCX binary executables
/// </summary>
public abstract class PoCXProcessWrapper : IDisposable
{
    protected readonly string _binaryPath;
    protected Process? _process;
    private readonly List<string> _outputLines = new();
    private readonly List<string> _errorLines = new();

    protected PoCXProcessWrapper(string binaryPath)
    {
        if (string.IsNullOrWhiteSpace(binaryPath))
            throw new ArgumentException("Binary path cannot be empty", nameof(binaryPath));

        if (!File.Exists(binaryPath))
            throw new FileNotFoundException($"PoCX binary not found at: {binaryPath}", binaryPath);

        _binaryPath = binaryPath;
    }

    /// <summary>
    /// Execute the binary with the specified arguments
    /// </summary>
    protected async Task<ProcessResult> ExecuteAsync(
        string arguments,
        CancellationToken cancellationToken = default,
        IProgress<string>? progress = null)
    {
        _outputLines.Clear();
        _errorLines.Clear();

        var startInfo = new ProcessStartInfo
        {
            FileName = _binaryPath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            CreateNoWindow = true
        };

        _process = new Process { StartInfo = startInfo };

        _process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                _outputLines.Add(e.Data);
                progress?.Report(e.Data);
            }
        };

        _process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                _errorLines.Add(e.Data);
            }
        };

        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        await _process.WaitForExitAsync(cancellationToken);

        return new ProcessResult
        {
            ExitCode = _process.ExitCode,
            Output = string.Join(Environment.NewLine, _outputLines),
            Error = string.Join(Environment.NewLine, _errorLines)
        };
    }

    /// <summary>
    /// Start the binary as a long-running process
    /// </summary>
    protected void StartProcess(string arguments, Action<string>? onOutput = null, Action<string>? onError = null)
    {
        if (_process != null && !_process.HasExited)
        {
            throw new InvalidOperationException("Process is already running");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = _binaryPath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            CreateNoWindow = true
        };

        _process = new Process { StartInfo = startInfo };

        if (onOutput != null)
        {
            _process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    onOutput(e.Data);
                }
            };
        }

        if (onError != null)
        {
            _process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    onError(e.Data);
                }
            };
        }

        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
    }

    /// <summary>
    /// Stop the running process
    /// </summary>
    public void StopProcess()
    {
        if (_process != null && !_process.HasExited)
        {
            _process.Kill(true);
            _process.WaitForExit(5000);
        }
    }

    /// <summary>
    /// Check if the process is running
    /// </summary>
    public bool IsRunning => _process != null && !_process.HasExited;

    public void Dispose()
    {
        StopProcess();
        _process?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Result from executing a process
/// </summary>
public class ProcessResult
{
    public int ExitCode { get; init; }
    public string Output { get; init; } = string.Empty;
    public string Error { get; init; } = string.Empty;
    public bool IsSuccess => ExitCode == 0;
}

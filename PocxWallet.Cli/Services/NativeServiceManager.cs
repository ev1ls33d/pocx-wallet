using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using Spectre.Console;

namespace PocxWallet.Cli.Services;

/// <summary>
/// Manages native processes for PoCX services (alternative to Docker containers)
/// </summary>
public class NativeServiceManager
{
    // Track running processes by service ID
    private readonly ConcurrentDictionary<string, ProcessInfo> _runningProcesses = new();
    
    // Maximum log size to display
    private const int MaxLogDisplaySize = 5000;
    
    // Process shutdown timeout in milliseconds
    private const int GracefulShutdownTimeoutMs = 5000;

    public NativeServiceManager()
    {
        // Constructor - no global logs directory needed
    }

    /// <summary>
    /// Start a native service process
    /// </summary>
    public async Task<bool> StartNativeServiceAsync(
        string serviceId,
        string serviceName,
        string binaryPath,
        string? arguments = null,
        string? workingDirectory = null,
        Dictionary<string, string>? environmentVars = null,
        bool spawnNewConsole = false)
    {
        try
        {
            // Check if already running
            if (_runningProcesses.TryGetValue(serviceId, out var existingProcess))
            {
                if (IsProcessAlive(existingProcess.Process))
                {
                    AnsiConsole.MarkupLine($"[yellow]Service '{serviceName}' is already running[/]");
                    return false;
                }
                else
                {
                    // Process died, remove it
                    _runningProcesses.TryRemove(serviceId, out _);
                }
            }

            // Validate binary exists
            if (!File.Exists(binaryPath))
            {
                AnsiConsole.MarkupLine($"[red]Binary not found:[/] {Markup.Escape(binaryPath)}");
                AnsiConsole.MarkupLine($"[yellow]Please download the binary using the version management menu[/]");
                return false;
            }

            // Determine working directory and log location
            var effectiveWorkingDir = workingDirectory ?? Path.GetDirectoryName(binaryPath) ?? ".";
            
            // Create logs directory in the service directory (not global logs/)
            var logsDir = Path.Combine(effectiveWorkingDir, "logs");
            Directory.CreateDirectory(logsDir);
            
            // Set up log files in service directory
            var logFilePath = Path.Combine(logsDir, $"{serviceId}.log");
            var errorLogFilePath = Path.Combine(logsDir, $"{serviceId}.error.log");

            // Create process start info
            var psi = new ProcessStartInfo
            {
                FileName = binaryPath,
                Arguments = arguments ?? "",
                WorkingDirectory = effectiveWorkingDir,
                UseShellExecute = spawnNewConsole,
                CreateNoWindow = !spawnNewConsole
            };
            
            // Only redirect streams if not spawning new console
            if (!spawnNewConsole)
            {
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.RedirectStandardInput = false;
            }

            // Add environment variables
            if (environmentVars != null)
            {
                foreach (var (key, value) in environmentVars)
                {
                    psi.Environment[key] = value;
                }
            }

            // Make binary executable on Unix systems
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    var chmodResult = await ExecuteCommandAsync("chmod", $"+x {binaryPath}");
                    if (chmodResult.exitCode != 0)
                    {
                        AnsiConsole.MarkupLine($"[yellow]Warning: Could not make binary executable[/]");
                    }
                }
                catch
                {
                    // Ignore chmod errors
                }
            }

            AnsiConsole.MarkupLine($"[bold]Starting native service:[/] {serviceName}");
            AnsiConsole.MarkupLine($"[dim]Binary:[/] {Markup.Escape(binaryPath)}");
            if (!string.IsNullOrEmpty(arguments))
            {
                AnsiConsole.MarkupLine($"[dim]Arguments:[/] {Markup.Escape(arguments)}");
            }
            AnsiConsole.MarkupLine($"[dim]Mode:[/] {(spawnNewConsole ? "New console window" : "Background with logs")}");
            AnsiConsole.MarkupLine($"[dim]Logs:[/] {Markup.Escape(logsDir)}");

            var process = new Process { StartInfo = psi };
            
            StreamWriter? logFile = null;
            StreamWriter? errorLogFile = null;

            // Only set up log file writers if not spawning new console
            if (!spawnNewConsole)
            {
                // Close any existing log files for this service
                CloseExistingLogFiles(serviceId);
                
                logFile = new StreamWriter(logFilePath, append: true) { AutoFlush = true };
                errorLogFile = new StreamWriter(errorLogFilePath, append: true) { AutoFlush = true };

                // Redirect output to log files
                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        logFile.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {e.Data}");
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        errorLogFile.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {e.Data}");
                    }
                };

                // Handle process exit
                process.Exited += (sender, e) =>
                {
                    AnsiConsole.MarkupLine($"[yellow]Service '{serviceName}' exited[/]");
                    logFile?.Dispose();
                    errorLogFile?.Dispose();
                    _runningProcesses.TryRemove(serviceId, out _);
                };

                process.EnableRaisingEvents = true;
            }
            else
            {
                // For new console, just track the process
                process.EnableRaisingEvents = true;
                process.Exited += (sender, e) =>
                {
                    _runningProcesses.TryRemove(serviceId, out _);
                };
            }

            // Start the process
            if (!process.Start())
            {
                AnsiConsole.MarkupLine($"[red]Failed to start process[/]");
                logFile?.Dispose();
                errorLogFile?.Dispose();
                return false;
            }

            // Begin async reading only if redirecting
            if (!spawnNewConsole)
            {
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }

            // Store process info
            var processInfo = new ProcessInfo
            {
                Process = process,
                ServiceId = serviceId,
                ServiceName = serviceName,
                BinaryPath = binaryPath,
                LogFile = logFile,
                ErrorLogFile = errorLogFile,
                StartTime = DateTime.Now,
                LogFilePath = logFilePath,
                ErrorLogFilePath = errorLogFilePath
            };

            _runningProcesses[serviceId] = processInfo;

            // Wait a moment to check if process started successfully
            await Task.Delay(500);

            if (!IsProcessAlive(process))
            {
                AnsiConsole.MarkupLine($"[red]Process exited immediately after starting[/]");
                if (!spawnNewConsole)
                {
                    AnsiConsole.MarkupLine($"[yellow]Check logs for details:[/] {Markup.Escape(errorLogFilePath)}");
                }
                return false;
            }

            AnsiConsole.MarkupLine($"[green]√[/] Service started successfully (PID: {process.Id})");
            return true;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error starting service:[/] {Markup.Escape(ex.Message)}");
            return false;
        }
    }

    /// <summary>
    /// Stop a native service process
    /// </summary>
    public async Task<bool> StopNativeServiceAsync(string serviceId, string serviceName)
    {
        if (!_runningProcesses.TryGetValue(serviceId, out var processInfo))
        {
            AnsiConsole.MarkupLine($"[yellow]Service '{serviceName}' is not running[/]");
            return false;
        }

        try
        {
            var process = processInfo.Process;

            if (!IsProcessAlive(process))
            {
                AnsiConsole.MarkupLine($"[yellow]Service '{serviceName}' is already stopped[/]");
                CleanupProcess(serviceId, processInfo);
                return true;
            }

            AnsiConsole.MarkupLine($"[bold]Stopping service:[/] {serviceName}");

            // Try graceful shutdown first
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // On Windows, send Ctrl+C if possible, otherwise CloseMainWindow
                    if (!process.CloseMainWindow())
                    {
                        process.Kill(entireProcessTree: false);
                    }
                }
                else
                {
                    // On Unix, send SIGTERM
                    process.Kill(entireProcessTree: false);
                }

                // Wait for graceful shutdown
                var shutdownTask = Task.Run(() => process.WaitForExit(GracefulShutdownTimeoutMs));
                if (await shutdownTask)
                {
                    AnsiConsole.MarkupLine($"[green]√[/] Service stopped gracefully");
                    CleanupProcess(serviceId, processInfo);
                    return true;
                }
            }
            catch
            {
                // Graceful shutdown failed, continue to force kill
            }

            // Force kill if graceful shutdown failed
            AnsiConsole.MarkupLine($"[yellow]Graceful shutdown timed out, forcing termination...[/]");
            try
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(2000);
                AnsiConsole.MarkupLine($"[green]√[/] Service terminated");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[yellow]Warning:[/] {Markup.Escape(ex.Message)}");
            }

            CleanupProcess(serviceId, processInfo);
            return true;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error stopping service:[/] {Markup.Escape(ex.Message)}");
            CleanupProcess(serviceId, processInfo);
            return false;
        }
    }

    /// <summary>
    /// Get the status of a native service
    /// </summary>
    public async Task<string> GetNativeServiceStatusAsync(string serviceId)
    {
        if (!_runningProcesses.TryGetValue(serviceId, out var processInfo))
        {
            return "not running";
        }

        if (IsProcessAlive(processInfo.Process))
        {
            return "running";
        }
        else
        {
            // Process died, clean up
            CleanupProcess(serviceId, processInfo);
            return "not running";
        }
    }

    /// <summary>
    /// Get logs from a native service
    /// </summary>
    public async Task<string> GetNativeServiceLogsAsync(string serviceId, int tailLines = 50)
    {
        // First try to get log paths from running process info
        string? logFilePath = null;
        string? errorLogFilePath = null;
        
        if (_runningProcesses.TryGetValue(serviceId, out var processInfo))
        {
            logFilePath = processInfo.LogFilePath;
            errorLogFilePath = processInfo.ErrorLogFilePath;
        }
        
        // Fallback to checking service directory
        if (string.IsNullOrEmpty(logFilePath))
        {
            var serviceDir = Path.Combine(".", serviceId);
            var logsDir = Path.Combine(serviceDir, "logs");
            logFilePath = Path.Combine(logsDir, $"{serviceId}.log");
            errorLogFilePath = Path.Combine(logsDir, $"{serviceId}.error.log");
        }

        var logs = new List<string>();

        // Read error log first
        if (!string.IsNullOrEmpty(errorLogFilePath) && File.Exists(errorLogFilePath))
        {
            try
            {
                var errorLines = await File.ReadAllLinesAsync(errorLogFilePath);
                if (errorLines.Length > 0)
                {
                    logs.Add("=== ERROR LOG ===");
                    logs.AddRange(errorLines.TakeLast(tailLines / 2));
                }
            }
            catch
            {
                // Ignore read errors
            }
        }

        // Read standard output log
        if (!string.IsNullOrEmpty(logFilePath) && File.Exists(logFilePath))
        {
            try
            {
                var outputLines = await File.ReadAllLinesAsync(logFilePath);
                if (outputLines.Length > 0)
                {
                    if (logs.Count > 0)
                    {
                        logs.Add("");
                        logs.Add("=== OUTPUT LOG ===");
                    }
                    logs.AddRange(outputLines.TakeLast(tailLines));
                }
            }
            catch
            {
                // Ignore read errors
            }
        }

        if (logs.Count == 0)
        {
            return "No logs available";
        }

        return string.Join(Environment.NewLine, logs);
    }

    /// <summary>
    /// Display service logs in a formatted panel
    /// </summary>
    public async Task DisplayNativeServiceLogsAsync(string serviceId, string serviceName, int tailLines = 50)
    {
        var status = await GetNativeServiceStatusAsync(serviceId);
        
        AnsiConsole.MarkupLine($"[bold]Service Status:[/] [{(status == "running" ? "green" : "yellow")}]{status}[/]");
        AnsiConsole.WriteLine();

        var logs = await GetNativeServiceLogsAsync(serviceId, tailLines);
        
        // Limit log display to reasonable size
        if (logs.Length > MaxLogDisplaySize)
        {
            logs = "...\n" + logs.Substring(logs.Length - MaxLogDisplaySize);
        }

        var escapedLogs = Markup.Escape(logs);

        var panel = new Panel(escapedLogs)
        {
            Header = new PanelHeader($"[bold]{serviceName} Logs (last {tailLines} lines)[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(status == "running" ? Color.Green : Color.Yellow)
        };
        
        AnsiConsole.Write(panel);
    }

    /// <summary>
    /// Download and extract a native binary
    /// </summary>
    public async Task<bool> DownloadAndExtractNativeAsync(
        string url,
        string version,
        string serviceId,
        string serviceName,
        List<string>? whitelist = null)
    {
        try
        {
            var serviceDir = Path.Combine(".", $"{serviceId}");
            Directory.CreateDirectory(serviceDir);

            var fileName = Path.GetFileName(new Uri(url).LocalPath);
            var downloadPath = Path.Combine(Path.GetTempPath(), fileName);

            AnsiConsole.MarkupLine($"[bold]Downloading {serviceName} {version}...[/]");
            AnsiConsole.MarkupLine($"[dim]URL:[/] {Markup.Escape(url)}");

            // Download the file
            using (var httpClient = new HttpClient())
            {
                httpClient.Timeout = TimeSpan.FromMinutes(30);

                await AnsiConsole.Progress()
                    .StartAsync(async ctx =>
                    {
                        var task = ctx.AddTask($"Downloading {fileName}", maxValue: 100);

                        using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                        response.EnsureSuccessStatusCode();

                        var totalBytes = response.Content.Headers.ContentLength ?? 0;
                        var canReportProgress = totalBytes > 0;

                        await using var contentStream = await response.Content.ReadAsStreamAsync();
                        await using var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                        var buffer = new byte[8192];
                        var totalRead = 0L;

                        while (true)
                        {
                            var bytesRead = await contentStream.ReadAsync(buffer);
                            if (bytesRead == 0) break;

                            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                            totalRead += bytesRead;

                            if (canReportProgress)
                            {
                                task.Value = (double)totalRead / totalBytes * 100;
                            }
                        }

                        task.Value = 100;
                    });
            }

            AnsiConsole.MarkupLine($"[green]√[/] Download complete");
            AnsiConsole.MarkupLine($"[bold]Extracting to {Markup.Escape(serviceDir)}...[/]");

            // Extract based on file extension
            if (fileName.EndsWith(".tar.gz") || fileName.EndsWith(".tgz"))
            {
                await ExtractTarGzAsync(downloadPath, serviceDir);
            }
            else if (fileName.EndsWith(".zip"))
            {
                ZipFile.ExtractToDirectory(downloadPath, serviceDir, overwriteFiles: true);
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Unsupported archive format:[/] {fileName}");
                return false;
            }

            AnsiConsole.MarkupLine($"[green]√[/] Extraction complete");

            // Apply whitelist filtering if provided
            if (whitelist != null && whitelist.Count > 0)
            {
                AnsiConsole.MarkupLine($"[bold]Applying whitelist filter...[/]");
                ApplyWhitelistFilter(serviceDir, whitelist);
                AnsiConsole.MarkupLine($"[green]√[/] Whitelist applied");
            }

            // Clean up downloaded archive
            File.Delete(downloadPath);

            AnsiConsole.MarkupLine($"[green]√[/] {serviceName} {version} installed successfully");
            return true;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error downloading/extracting:[/] {Markup.Escape(ex.Message)}");
            return false;
        }
    }

    /// <summary>
    /// Extract .tar.gz archive
    /// </summary>
    private async Task ExtractTarGzAsync(string archivePath, string destinationPath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // On Windows, try using tar command (available in Windows 10+)
            try
            {
                var result = await ExecuteCommandAsync("tar", $"-xzf \"{archivePath}\" -C \"{destinationPath}\"");
                if (result.exitCode != 0)
                {
                    throw new Exception($"tar extraction failed: {result.output}. On older Windows systems, you may need to install a tool like 7-Zip or manually extract the archive.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to extract tar.gz on Windows. Please ensure tar is available (Windows 10+ includes it) or manually extract to: {destinationPath}. Error: {ex.Message}");
            }
        }
        else
        {
            // On Unix, use tar command
            var result = await ExecuteCommandAsync("tar", $"-xzf \"{archivePath}\" -C \"{destinationPath}\"");
            if (result.exitCode != 0)
            {
                throw new Exception($"Failed to extract tar.gz: {result.output}");
            }
        }
    }

    /// <summary>
    /// Apply whitelist filter - delete all files NOT in the whitelist
    /// </summary>
    private void ApplyWhitelistFilter(string directory, List<string> whitelist)
    {
        var allFiles = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
        var deletedCount = 0;

        // Create HashSet for O(1) lookups
        var comparer = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
        var whitelistSet = new HashSet<string>(whitelist, comparer);

        foreach (var file in allFiles)
        {
            var relativePath = Path.GetRelativePath(directory, file);
            var fileName = Path.GetFileName(file);
            
            // Check if file or relative path is in whitelist using HashSet
            if (!whitelistSet.Contains(fileName) && !whitelistSet.Contains(relativePath))
            {
                try
                {
                    File.Delete(file);
                    deletedCount++;
                }
                catch
                {
                    // Ignore deletion errors
                }
            }
        }

        AnsiConsole.MarkupLine($"[dim]Removed {deletedCount} non-whitelisted files[/]");

        // Remove empty directories
        RemoveEmptyDirectories(directory);
    }

    /// <summary>
    /// Remove empty directories recursively
    /// </summary>
    private void RemoveEmptyDirectories(string directory)
    {
        foreach (var subDir in Directory.GetDirectories(directory))
        {
            RemoveEmptyDirectories(subDir);
            if (!Directory.EnumerateFileSystemEntries(subDir).Any())
            {
                try
                {
                    Directory.Delete(subDir);
                }
                catch
                {
                    // Ignore deletion errors
                }
            }
        }
    }

    /// <summary>
    /// Get the current platform identifier for filtering downloads
    /// </summary>
    public static string GetCurrentPlatform()
    {
        var os = "";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            os = "win";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            os = "linux";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            os = "osx";
        else
            os = "unknown";

        var arch = RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            Architecture.Arm64 => "arm64",
            Architecture.Arm => "arm",
            _ => "unknown"
        };

        return $"{os}-{arch}";
    }

    /// <summary>
    /// Check if a process is alive
    /// </summary>
    private bool IsProcessAlive(Process process)
    {
        try
        {
            return !process.HasExited;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Clean up process resources
    /// </summary>
    private void CleanupProcess(string serviceId, ProcessInfo processInfo)
    {
        try
        {
            processInfo.LogFile?.Dispose();
            processInfo.ErrorLogFile?.Dispose();
            processInfo.Process?.Dispose();
        }
        catch
        {
            // Ignore cleanup errors
        }

        _runningProcesses.TryRemove(serviceId, out _);
    }

    /// <summary>
    /// Close existing log files for a service to prevent file locking issues
    /// </summary>
    private void CloseExistingLogFiles(string serviceId)
    {
        if (_runningProcesses.TryGetValue(serviceId, out var existingProcess))
        {
            try
            {
                existingProcess.LogFile?.Dispose();
                existingProcess.ErrorLogFile?.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
        }
    }

    /// <summary>
    /// Execute a shell command
    /// </summary>
    private async Task<(int exitCode, string output)> ExecuteCommandAsync(string command, string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        
        await process.WaitForExitAsync();
        
        var output = await outputTask;
        var error = await errorTask;

        var combinedOutput = string.IsNullOrWhiteSpace(error) ? output : $"{output}\n{error}".Trim();

        return (process.ExitCode, combinedOutput);
    }

    /// <summary>
    /// Information about a running process
    /// </summary>
    private class ProcessInfo
    {
        public Process Process { get; set; } = null!;
        public string ServiceId { get; set; } = "";
        public string ServiceName { get; set; } = "";
        public string BinaryPath { get; set; } = "";
        public StreamWriter? LogFile { get; set; }
        public StreamWriter? ErrorLogFile { get; set; }
        public DateTime StartTime { get; set; }
        public string LogFilePath { get; set; } = "";
        public string ErrorLogFilePath { get; set; } = "";
    }
}

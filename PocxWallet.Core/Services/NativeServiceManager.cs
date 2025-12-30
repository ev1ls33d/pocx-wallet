using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace PocxWallet.Core.Services;

/// <summary>
/// Manages native processes for PoCX services (alternative to Docker containers)
/// </summary>
public class NativeServiceManager
{
    private readonly ConcurrentDictionary<string, ProcessInfo> _runningProcesses = new();
    private const int GracefulShutdownTimeoutMs = 5000;
    private readonly IServiceLogger _logger;

    public NativeServiceManager() : this(SilentServiceLogger.Instance)
    {
    }

    public NativeServiceManager(IServiceLogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Start a native service process
    /// </summary>
    public async Task<bool> StartNativeServiceAsync(
        string serviceId,
        string serviceName,
        string binaryName,
        string? arguments = null,
        string? workingDirectory = null,
        Dictionary<string, string>? environmentVars = null,
        bool spawnNewConsole = false)
    {
        try
        {
            if (_runningProcesses.TryGetValue(serviceId, out var existingProcess))
            {
                if (IsProcessAlive(existingProcess.Process))
                {
                    _logger.LogWarning($"Service '{serviceName}' is already running");
                    return false;
                }
                else
                {
                    _runningProcesses.TryRemove(serviceId, out _);
                }
            }

            if (string.IsNullOrEmpty(workingDirectory) || !File.Exists(Path.Combine(workingDirectory, binaryName)))
            {
                _logger.LogError($"Binary not found: {binaryName}");
                _logger.LogWarning("Please download the binary using the version management menu");
                return false;
            }

            var effectiveWorkingDir = workingDirectory ?? Path.GetDirectoryName(binaryName) ?? ".";

            var psi = new ProcessStartInfo
            {
                FileName = binaryName,
                Arguments = arguments ?? "",
                WorkingDirectory = effectiveWorkingDir,
                UseShellExecute = spawnNewConsole,
                CreateNoWindow = !spawnNewConsole,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                RedirectStandardInput = false
            };

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    // Make binary executable on Unix - use full path
                    var fullBinaryPath = Path.Combine(effectiveWorkingDir, binaryName);
                    var chmodResult = await ExecuteCommandAsync("chmod", $"+x \"{fullBinaryPath}\"");
                    if (chmodResult.exitCode != 0)
                    {
                        _logger.LogWarning("Warning: Could not make binary executable");
                    }
                }
                catch { }
            }
            else
            {
                // On Windows, use cmd.exe with the full path to the binary
                var fullBinaryPath = Path.GetFullPath(Path.Combine(effectiveWorkingDir, binaryName));
                psi.FileName = "cmd.exe";
                psi.Arguments = $"/c \"\"{fullBinaryPath}\" {arguments ?? ""} || pause;exit\"";
            }

            _logger.LogInfo($"Starting native service: {serviceName}");
            _logger.LogDebug($"Binary: {Path.Combine(effectiveWorkingDir, binaryName)}");
            if (!string.IsNullOrEmpty(arguments))
            {
                _logger.LogDebug($"Arguments: {arguments}");
            }
            _logger.LogDebug($"Mode: {(spawnNewConsole ? "New console window" : "Background process")}");

            var process = new Process { StartInfo = psi };
            
            process.EnableRaisingEvents = true;
            process.Exited += (sender, e) =>
            {
                _logger.LogWarning($"Service '{serviceName}' exited");
                _runningProcesses.TryRemove(serviceId, out _);
            };

            if (!process.Start())
            {
                _logger.LogError("Failed to start process");
                return false;
            }

            var processInfo = new ProcessInfo
            {
                Process = process,
                ServiceId = serviceId,
                ServiceName = serviceName,
                BinaryPath = binaryName,
                StartTime = DateTime.Now
            };

            _runningProcesses[serviceId] = processInfo;

            await Task.Delay(500);

            if (!IsProcessAlive(process))
            {
                _logger.LogError("Process exited immediately after starting");
                return false;
            }

            _logger.LogSuccess($"Service started successfully (PID: {process.Id})");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error starting service: {ex.Message}");
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
            _logger.LogWarning($"Service '{serviceName}' is not running");
            return false;
        }

        try
        {
            var process = processInfo.Process;

            if (!IsProcessAlive(process))
            {
                _logger.LogWarning($"Service '{serviceName}' is already stopped");
                CleanupProcess(serviceId, processInfo);
                return true;
            }

            _logger.LogInfo($"Stopping service: {serviceName}");

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    if (!process.CloseMainWindow())
                    {
                        process.Kill(entireProcessTree: false);
                    }
                }
                else
                {
                    process.Kill(entireProcessTree: false);
                }

                var shutdownTask = Task.Run(() => process.WaitForExit(GracefulShutdownTimeoutMs));
                if (await shutdownTask)
                {
                    _logger.LogSuccess("Service stopped gracefully");
                    CleanupProcess(serviceId, processInfo);
                    return true;
                }
            }
            catch { }

            _logger.LogWarning("Graceful shutdown timed out, forcing termination...");
            try
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(2000);
                _logger.LogSuccess("Service terminated");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Warning: {ex.Message}");
            }

            CleanupProcess(serviceId, processInfo);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error stopping service: {ex.Message}");
            CleanupProcess(serviceId, processInfo);
            return false;
        }
    }

    /// <summary>
    /// Get the status of a native service
    /// </summary>
    public Task<string> GetNativeServiceStatusAsync(string serviceId)
    {
        if (!_runningProcesses.TryGetValue(serviceId, out var processInfo))
        {
            return Task.FromResult("not running");
        }

        if (IsProcessAlive(processInfo.Process))
        {
            return Task.FromResult("running");
        }
        else
        {
            CleanupProcess(serviceId, processInfo);
            return Task.FromResult("not running");
        }
    }

    /// <summary>
    /// Download and extract a native binary
    /// </summary>
    public async Task<bool> DownloadAndExtractNativeAsync(
        string url,
        string version,
        string serviceId,
        string serviceName,
        List<string>? whitelist = null,
        Action<double>? progressCallback = null)
    {
        try
        {
            var serviceDir = Path.Combine(".", $"{serviceId}");
            Directory.CreateDirectory(serviceDir);

            var fileName = Path.GetFileName(new Uri(url).LocalPath);
            var downloadPath = Path.Combine(Path.GetTempPath(), fileName);

            _logger.LogInfo($"Downloading {serviceName} {version}...");
            _logger.LogDebug($"URL: {url}");

            using (var httpClient = new HttpClient())
            {
                httpClient.Timeout = TimeSpan.FromMinutes(30);

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
                        progressCallback?.Invoke((double)totalRead / totalBytes * 100);
                    }
                }
            }

            _logger.LogSuccess("Download complete");
            _logger.LogInfo($"Extracting to: {serviceDir}");

            if (fileName.EndsWith(".tar.gz") || fileName.EndsWith(".tgz"))
            {
                await ExtractTarGzAsync(downloadPath, serviceDir);
            }
            else if (fileName.EndsWith(".zip"))
            {
                ZipFile.ExtractToDirectory(downloadPath, serviceDir, overwriteFiles: true);
            }
            else if (fileName.EndsWith(".exe"))
            {
                _logger.LogDebug("Checking if executable is an archive...");
                
                if (await TryExtractExeAsArchiveAsync(downloadPath, serviceDir))
                {
                    _logger.LogDebug("Detected as self-extracting archive");
                }
                else
                {
                    _logger.LogError($"Unsupported archive format: {fileName}");
                    return false;
                }
            }
            else
            {
                _logger.LogError($"Unsupported archive format: {fileName}");
                return false;
            }

            _logger.LogSuccess("Extraction complete");

            if (whitelist != null && whitelist.Count > 0)
            {
                _logger.LogInfo("Applying whitelist filter...");
                MoveWhitelistedFiles(serviceDir, whitelist);
                _logger.LogSuccess("Whitelist applied");
            }

            File.Delete(downloadPath);

            _logger.LogSuccess($"{serviceName} {version} installed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error downloading: {ex.Message}");
            return false;
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

    private async Task ExtractTarGzAsync(string archivePath, string destinationPath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                var result = await ExecuteCommandAsync("tar", $"-xzf \"{archivePath}\" -C \"{destinationPath}\"");
                if (result.exitCode != 0)
                {
                    throw new Exception($"tar extraction failed: {result.output}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to extract tar.gz on Windows: {ex.Message}");
            }
        }
        else
        {
            var result = await ExecuteCommandAsync("tar", $"-xzf \"{archivePath}\" -C \"{destinationPath}\"");
            if (result.exitCode != 0)
            {
                throw new Exception($"Failed to extract tar.gz: {result.output}");
            }
        }
    }

    private async Task<bool> TryExtractExeAsArchiveAsync(string exePath, string destinationPath)
    {
        try
        {
            var result = await ExecuteCommandAsync("7z", $"x \"{exePath}\" -o\"{destinationPath}\" -y");
            if (result.exitCode == 0)
            {
                return true;
            }
        }
        catch { }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                var result = await ExecuteCommandAsync("expand", $"\"{exePath}\" -F:* \"{destinationPath}\"");
                if (result.exitCode == 0)
                {
                    return true;
                }
            }
            catch { }
        }

        try
        {
            ZipFile.ExtractToDirectory(exePath, destinationPath, overwriteFiles: true);
            return true;
        }
        catch { }

        return false;
    }

    private void MoveWhitelistedFiles(string directory, List<string> whitelist)
    {
        var allFiles = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
        var movedCount = 0;
        var tempDir = Path.Combine(Path.GetTempPath(), $"whitelist_temp_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var comparer = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal;
            var whitelistSet = new HashSet<string>(whitelist, comparer);

            _logger.LogDebug("Moving whitelisted files...");

            foreach (var file in allFiles)
            {
                var fileName = Path.GetFileName(file);
                
                if (whitelistSet.Contains(fileName))
                {
                    var destPath = Path.Combine(tempDir, fileName);
                    File.Copy(file, destPath, overwrite: true);
                    movedCount++;
                }
            }

            if (movedCount == 0)
            {
                _logger.LogWarning("No whitelisted files found");
            }
            else
            {
                Directory.Delete(directory, recursive: true);
                Directory.Move(tempDir, directory);
                _logger.LogDebug($"Moved {movedCount} files");
            }
        }
        catch (Exception ex)
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, recursive: true); } catch { }
            }
            throw new Exception($"Error applying whitelist: {ex.Message}");
        }
    }

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

    private void CleanupProcess(string serviceId, ProcessInfo processInfo)
    {
        try
        {
            processInfo.Process?.Dispose();
        }
        catch { }

        _runningProcesses.TryRemove(serviceId, out _);
    }

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

    private class ProcessInfo
    {
        public Process Process { get; set; } = null!;
        public string ServiceId { get; set; } = "";
        public string ServiceName { get; set; } = "";
        public string BinaryPath { get; set; } = "";
        public DateTime StartTime { get; set; }
    }
}

using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using PocxWallet.Cli.Resources;
using Spectre.Console;

namespace PocxWallet.Cli.Services;

/// <summary>
/// Manages native processes for PoCX services (alternative to Docker containers)
/// </summary>
public class NativeServiceManager
{
    // Track running processes by service ID
    private readonly ConcurrentDictionary<string, ProcessInfo> _runningProcesses = new();
    
    // Process shutdown timeout in milliseconds
    private const int GracefulShutdownTimeoutMs = 5000;

    public NativeServiceManager()
    {
        // Constructor
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
            if (string.IsNullOrEmpty(workingDirectory) || !File.Exists(Path.Combine(workingDirectory, binaryName)))
            {
                AnsiConsole.MarkupLine($"[red]Binary not found:[/] {Markup.Escape(binaryName)}");
                AnsiConsole.MarkupLine($"[yellow]Please download the binary using the version management menu[/]");
                return false;
            }

            // Determine working directory
            var effectiveWorkingDir = workingDirectory ?? Path.GetDirectoryName(binaryName) ?? ".";

            // Create process start info
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

            /*
            // Add environment variables
            if (environmentVars != null)
            {
                foreach (var (key, value) in environmentVars)
                {
                    psi.Environment[key] = value;
                }
            }
            */

            // Make binary executable on Unix systems
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    var chmodResult = await ExecuteCommandAsync("chmod", $"+x {binaryName}");
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
            else
            {
                psi.FileName = "cmd.exe";
                psi.Arguments = $"/c \"{binaryName} {arguments ?? ""} || pause;exit\""; //  Keep console open on error
            }

            AnsiConsole.MarkupLine($"[bold]Starting native service:[/] {serviceName}");
            AnsiConsole.MarkupLine($"[dim]Binary:[/] {Markup.Escape(binaryName)}");
            if (!string.IsNullOrEmpty(arguments))
            {
                AnsiConsole.MarkupLine($"[dim]Arguments:[/] {Markup.Escape(arguments)}");
            }
            AnsiConsole.MarkupLine($"[dim]Mode:[/] {(spawnNewConsole ? "New console window" : "Background process")}");

            var process = new Process { StartInfo = psi };
            
            // Track process exit
            process.EnableRaisingEvents = true;
            process.Exited += (sender, e) =>
            {
                AnsiConsole.MarkupLine($"[yellow]Service '{serviceName}' exited[/]");
                _runningProcesses.TryRemove(serviceId, out _);
            };

            // Start the process
            if (!process.Start())
            {
                AnsiConsole.MarkupLine($"[red]Failed to start process[/]");
                return false;
            }

            // Store process info
            var processInfo = new ProcessInfo
            {
                Process = process,
                ServiceId = serviceId,
                ServiceName = serviceName,
                BinaryPath = binaryName,
                StartTime = DateTime.Now
            };

            _runningProcesses[serviceId] = processInfo;

            // Wait a moment to check if process started successfully
            await Task.Delay(500);

            if (!IsProcessAlive(process))
            {
                AnsiConsole.MarkupLine($"[red]Process exited immediately after starting[/]");
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

            AnsiConsole.MarkupLine(Strings.NativeService.DownloadComplete);
            AnsiConsole.MarkupLine(string.Format(Strings.NativeService.ExtractingToFormat, Markup.Escape(serviceDir)));

            // Extract based on file extension
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
                // Check if the .exe is actually an archive (like setup.exe)
                AnsiConsole.MarkupLine(Strings.NativeService.CheckingIfArchive);
                
                if (await TryExtractExeAsArchiveAsync(downloadPath, serviceDir))
                {
                    AnsiConsole.MarkupLine(Strings.NativeService.DetectedAsArchive);
                }
                else
                {
                    AnsiConsole.MarkupLine(Strings.NativeService.NotAnArchive);
                    AnsiConsole.MarkupLine(string.Format(Strings.NativeService.UnsupportedArchiveFormat, fileName));
                    return false;
                }
            }
            else
            {
                AnsiConsole.MarkupLine(string.Format(Strings.NativeService.UnsupportedArchiveFormat, fileName));
                return false;
            }

            AnsiConsole.MarkupLine(Strings.NativeService.ExtractionComplete);

            // Apply whitelist filtering if provided
            if (whitelist != null && whitelist.Count > 0)
            {
                AnsiConsole.MarkupLine(Strings.NativeService.ApplyingWhitelist);
                MoveWhitelistedFiles(serviceDir, whitelist);
                AnsiConsole.MarkupLine(Strings.NativeService.WhitelistApplied);
            }

            // Clean up downloaded archive
            File.Delete(downloadPath);

            AnsiConsole.MarkupLine(string.Format(Strings.NativeService.InstalledSuccessFormat, serviceName, version));
            return true;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine(string.Format(Strings.NativeService.ErrorDownloadingFormat, Markup.Escape(ex.Message)));
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
    /// Try to extract .exe file as an archive using 7-Zip or similar tools
    /// </summary>
    private async Task<bool> TryExtractExeAsArchiveAsync(string exePath, string destinationPath)
    {
        // Try using 7z command if available
        try
        {
            var result = await ExecuteCommandAsync("7z", $"x \"{exePath}\" -o\"{destinationPath}\" -y");
            if (result.exitCode == 0)
            {
                return true;
            }
        }
        catch
        {
            // 7z not available, try alternative methods
        }

        // On Windows, try using expand command for self-extracting archives
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
            catch
            {
                // expand failed
            }
        }

        // Try as ZIP archive (some .exe files are actually ZIP files)
        try
        {
            ZipFile.ExtractToDirectory(exePath, destinationPath, overwriteFiles: true);
            return true;
        }
        catch
        {
            // Not a ZIP file
        }

        return false;
    }

    /// <summary>
    /// Move whitelisted files from extracted directory to target directory
    /// Instead of deleting non-whitelisted files, we move only the whitelisted ones
    /// This handles cases where binaries are in subdirectories
    /// </summary>
    private void MoveWhitelistedFiles(string directory, List<string> whitelist)
    {
        var allFiles = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
        var movedCount = 0;
        var tempDir = Path.Combine(Path.GetTempPath(), $"whitelist_temp_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create HashSet for O(1) lookups
            var comparer = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal;
            var whitelistSet = new HashSet<string>(whitelist, comparer);

            AnsiConsole.MarkupLine(Strings.NativeService.MovingWhitelistedFiles);

            // Move whitelisted files to temp directory (keeping only filename, not subdirectory structure)
            foreach (var file in allFiles)
            {
                var fileName = Path.GetFileName(file);
                
                // Check if filename is in whitelist
                if (whitelistSet.Contains(fileName))
                {
                    var destPath = Path.Combine(tempDir, fileName);
                    File.Copy(file, destPath, overwrite: true);
                    movedCount++;
                }
            }

            if (movedCount == 0)
            {
                AnsiConsole.MarkupLine(Strings.NativeService.NoWhitelistedFilesFound);
            }
            else
            {
                // Delete original extraction directory
                Directory.Delete(directory, recursive: true);
                
                // Move temp directory to target location
                Directory.Move(tempDir, directory);
                
                AnsiConsole.MarkupLine(string.Format(Strings.NativeService.MovedFilesFormat, movedCount));
            }
        }
        catch (Exception ex)
        {
            // Clean up temp directory on error
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, recursive: true); } catch { }
            }
            throw new Exception($"Error applying whitelist: {ex.Message}");
        }
    }

    /// <summary>
    /// Apply whitelist filter - delete all files NOT in the whitelist (legacy method, kept for compatibility)
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
            processInfo.Process?.Dispose();
        }
        catch
        {
            // Ignore cleanup errors
        }

        _runningProcesses.TryRemove(serviceId, out _);
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
        public DateTime StartTime { get; set; }
    }
}

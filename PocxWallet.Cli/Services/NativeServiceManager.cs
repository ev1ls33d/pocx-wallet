using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using PocxWallet.Cli.Resources;
using Spectre.Console;

namespace PocxWallet.Cli.Services;

public class NativeServiceManager
{
    private readonly ConcurrentDictionary<string, ProcessInfo> _runningProcesses = new();
    private const int GracefulShutdownTimeoutMs = 5000;

    public NativeServiceManager() { }

    public async Task<bool> StartNativeServiceAsync(string serviceId, string serviceName, string binaryName, string? arguments = null, string? workingDirectory = null, Dictionary<string, string>? environmentVars = null, bool spawnNewConsole = false)
    {
        try {
            if (_runningProcesses.TryGetValue(serviceId, out var existingProcess)) {
                if (IsProcessAlive(existingProcess.Process)) {
                    AnsiConsole.MarkupLine($"[yellow]Service '{serviceName}' is already running[/]");
                    return false;
                }
                _runningProcesses.TryRemove(serviceId, out _);
            }

            var effectiveWorkingDir = workingDirectory ?? ".";
            var binaryPath = Path.Combine(effectiveWorkingDir, binaryName);

            if (!File.Exists(binaryPath)) {
                AnsiConsole.MarkupLine(string.Format(Strings.ExternalNode.BinaryNotFoundFormat, effectiveWorkingDir));
                return false;
            }

            var psi = new ProcessStartInfo {
                FileName = binaryPath,
                Arguments = arguments ?? "",
                WorkingDirectory = effectiveWorkingDir,
                UseShellExecute = spawnNewConsole,
                CreateNoWindow = !spawnNewConsole,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                RedirectStandardInput = false
            };

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                try { await ExecuteCommandAsync("chmod", "+x " + binaryPath); } catch { }
            }

            AnsiConsole.MarkupLine(string.Format(Strings.NativeService.StartingFormat, serviceName));

            var process = Process.Start(psi);
            if (process == null) {
                AnsiConsole.MarkupLine(Strings.NativeService.FailedToStart);
                return false;
            }

            _runningProcesses[serviceId] = new ProcessInfo { Process = process, ServiceId = serviceId, ServiceName = serviceName, BinaryPath = binaryPath, StartTime = DateTime.Now };
            
            await Task.Delay(500);
            if (IsProcessAlive(process)) {
                AnsiConsole.MarkupLine(string.Format(Strings.NativeService.StartedSuccessFormat, process.Id));
                return true;
            } else {
                AnsiConsole.MarkupLine(Strings.NativeService.ExitedImmediately);
                return false;
            }
        } catch (Exception ex) { 
            AnsiConsole.MarkupLine(string.Format(Strings.NativeService.ErrorDownloadingFormat, Markup.Escape(ex.Message)));
            return false; 
        }
    }

    public async Task<bool> StopNativeServiceAsync(string serviceId, string serviceName)
    {
        if (!_runningProcesses.TryGetValue(serviceId, out var processInfo)) {
            AnsiConsole.MarkupLine($"[yellow]Service '{serviceName}' is not running[/]");
            return false;
        }

        try {
            var process = processInfo.Process;
            if (!IsProcessAlive(process)) { 
                AnsiConsole.MarkupLine($"[yellow]Service '{serviceName}' is already stopped[/]");
                CleanupProcess(serviceId, processInfo); 
                return true; 
            }

            AnsiConsole.MarkupLine(string.Format(Strings.Container.StoppingFormat, serviceName));

            try {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { if (!process.CloseMainWindow()) process.Kill(false); }
                else process.Kill(false);
                var shutdownTask = Task.Run(() => process.WaitForExit(GracefulShutdownTimeoutMs));
                if (await shutdownTask) { 
                    AnsiConsole.MarkupLine(Strings.NativeService.StoppedGracefully);
                    CleanupProcess(serviceId, processInfo); 
                    return true; 
                }
            } catch { }

            AnsiConsole.MarkupLine(Strings.NativeService.ShutdownTimeout);
            process.Kill(true);
            process.WaitForExit(2000);
            AnsiConsole.MarkupLine(Strings.NativeService.Terminated);
            CleanupProcess(serviceId, processInfo);
            return true;
        } catch (Exception ex) { 
            AnsiConsole.MarkupLine($"[red]Error stopping service:[/] {Markup.Escape(ex.Message)}");
            CleanupProcess(serviceId, processInfo); 
            return false; 
        }
    }

    public async Task<string> GetNativeServiceStatusAsync(string serviceId)
    {
        if (!_runningProcesses.TryGetValue(serviceId, out var processInfo)) return "not running";
        if (IsProcessAlive(processInfo.Process)) return "running";
        CleanupProcess(serviceId, processInfo);
        return "not running";
    }

    public async Task<bool> DownloadAndExtractNativeAsync(string url, string version, string serviceId, string serviceName, List<string>? whitelist = null)
    {
        try {
            var serviceDir = Path.Combine(".", serviceId);
            Directory.CreateDirectory(serviceDir);
            var fileName = Path.GetFileName(new Uri(url).LocalPath);
            var downloadPath = Path.Combine(Path.GetTempPath(), fileName);

            AnsiConsole.MarkupLine($"[bold]Downloading {serviceName} {version}...[/]");
            AnsiConsole.MarkupLine($"[dim]URL:[/] {Markup.Escape(url)}");

            using (var httpClient = new HttpClient()) {
                httpClient.Timeout = TimeSpan.FromMinutes(30);
                await AnsiConsole.Progress().StartAsync(async ctx => {
                    var task = ctx.AddTask($"Downloading {fileName}", true, 100);
                    using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();
                    var totalBytes = response.Content.Headers.ContentLength ?? 0;
                    await using var contentStream = await response.Content.ReadAsStreamAsync();
                    await using var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
                    var buffer = new byte[8192];
                    var totalRead = 0L;
                    while (true) {
                        var bytesRead = await contentStream.ReadAsync(buffer);
                        if (bytesRead == 0) break;
                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                        totalRead += bytesRead;
                        if (totalBytes > 0) task.Value = (double)totalRead / totalBytes * 100;
                    }
                });
            }

            AnsiConsole.MarkupLine(Strings.NativeService.DownloadComplete);
            AnsiConsole.MarkupLine(string.Format(Strings.NativeService.ExtractingToFormat, Markup.Escape(serviceDir)));

            if (fileName.EndsWith(".tar.gz") || fileName.EndsWith(".tgz")) await ExtractTarGzAsync(downloadPath, serviceDir);
            else if (fileName.EndsWith(".zip")) ZipFile.ExtractToDirectory(downloadPath, serviceDir, true);
            else if (fileName.EndsWith(".exe")) { 
                AnsiConsole.MarkupLine(Strings.NativeService.CheckingIfArchive);
                if (!await TryExtractExeAsArchiveAsync(downloadPath, serviceDir)) {
                    AnsiConsole.MarkupLine(Strings.NativeService.NotAnArchive);
                    return false; 
                }
                AnsiConsole.MarkupLine(Strings.NativeService.DetectedAsArchive);
            }
            else return false;

            AnsiConsole.MarkupLine(Strings.NativeService.ExtractionComplete);

            if (whitelist != null && whitelist.Count > 0) { 
                AnsiConsole.MarkupLine(Strings.NativeService.MovingWhitelistedFiles);
                if (!MoveWhitelistedFiles(serviceDir, whitelist)) {
                    AnsiConsole.MarkupLine(string.Format(Strings.NativeService.ErrorDownloadingFormat, "No whitelisted files found."));
                    return false; 
                }
                AnsiConsole.MarkupLine(Strings.NativeService.WhitelistApplied);
            }

            File.Delete(downloadPath);
            AnsiConsole.MarkupLine(string.Format(Strings.NativeService.InstalledSuccessFormat, serviceName, version));
            return true;
        } catch (Exception ex) { 
            AnsiConsole.MarkupLine(string.Format(Strings.NativeService.ErrorDownloadingFormat, Markup.Escape(ex.Message))); 
            return false; 
        }
    }

    private async Task ExtractTarGzAsync(string archivePath, string destinationPath)
    {
        await ExecuteCommandAsync("tar", $"-xzf \"{archivePath}\" -C \"{destinationPath}\" ");
    }

    private async Task<bool> TryExtractExeAsArchiveAsync(string exePath, string destinationPath)
    {
        try {
            var result = await ExecuteCommandAsync("7z", $"x \"{exePath}\" -o\"{destinationPath}\" -y");
            if (result.exitCode == 0 && Directory.GetFiles(destinationPath, "*", SearchOption.AllDirectories).Length > 0) return true;
        } catch { }
        try {
            ZipFile.ExtractToDirectory(exePath, destinationPath, true);
            if (Directory.GetFiles(destinationPath, "*", SearchOption.AllDirectories).Length > 0) return true;
        } catch { }
        return false;
    }

    private bool MoveWhitelistedFiles(string directory, List<string> whitelist)
    {
        AnsiConsole.MarkupLine(Strings.NativeService.MovingWhitelistedFiles);
        var allFiles = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
        var tempDir = Path.Combine(Path.GetTempPath(), $"whitelist_temp_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        var whitelistSet = new HashSet<string>(whitelist, StringComparer.OrdinalIgnoreCase);
        var moved = 0;
        foreach (var file in allFiles) {
            if (whitelistSet.Contains(Path.GetFileName(file))) {
                File.Copy(file, Path.Combine(tempDir, Path.GetFileName(file)), true);
                moved++;
            }
        }
        if (moved > 0) {
            Directory.Delete(directory, true);
            Directory.Move(tempDir, directory);
            AnsiConsole.MarkupLine(string.Format(Strings.NativeService.MovedFilesFormat, moved));
            return true;
        }
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        AnsiConsole.MarkupLine(Strings.NativeService.NoWhitelistedFilesFound);
        return false;
    }

    public static string GetCurrentPlatform()
    {
        var os = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win" : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux" : "osx";
        var arch = RuntimeInformation.OSArchitecture switch { Architecture.X64 => "x64", Architecture.Arm64 => "arm64", _ => "unknown" };
        return os + "-" + arch;
    }

    private bool IsProcessAlive(Process p) { try { return !p.HasExited; } catch { return false; } }
    private void CleanupProcess(string id, ProcessInfo info) { try { info.Process?.Dispose(); } catch { } _runningProcesses.TryRemove(id, out _); }
    private async Task<(int exitCode, string output)> ExecuteCommandAsync(string command, string arguments)
    {
        var psi = new ProcessStartInfo { 
            FileName = command, 
            RedirectStandardOutput = true, 
            RedirectStandardError = true, 
            UseShellExecute = false, 
            CreateNoWindow = true 
        };

        // Split arguments but respect quotes to preserve JSON
        var args = CommandLineHelper.SplitCommandLineArguments(arguments);
        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        using var p = Process.Start(psi);
        if (p == null) return (1, "");
        var o = await p.StandardOutput.ReadToEndAsync();
        var e = await p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync();
        return (p.ExitCode, string.IsNullOrWhiteSpace(e) ? o : $"{o}\n{e}".Trim());
    }
    private class ProcessInfo { public Process Process { get; set; } = null!; public string ServiceId { get; set; } = ""; public string ServiceName { get; set; } = ""; public string BinaryPath { get; set; } = ""; public DateTime StartTime { get; set; } }
}
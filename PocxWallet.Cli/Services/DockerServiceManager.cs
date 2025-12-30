using System.Diagnostics;
using System.Runtime.InteropServices;
using PocxWallet.Core.Services;
using Spectre.Console;

namespace PocxWallet.Cli.Services;

/// <summary>
/// CLI-specific Docker service manager with Spectre.Console output
/// Extends the Core DockerManager with CLI-specific features
/// </summary>
public class DockerServiceManager : DockerManager
{
    private const int MaxLogDisplaySize = 5000;

    public DockerServiceManager() : base(SpectreServiceLogger.Instance)
    {
    }

    /// <summary>
    /// Install Docker (or guide user to install) - CLI-specific with interactive prompts
    /// </summary>
    public async Task<bool> EnsureDockerInstalledAsync()
    {
        if (await IsDockerAvailableAsync())
        {
            AnsiConsole.MarkupLine("[green]√[/] Docker is installed and running");
            return true;
        }

        AnsiConsole.MarkupLine("[yellow]Docker is not installed or not running[/]");
        AnsiConsole.WriteLine();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            AnsiConsole.MarkupLine("[bold]To use Docker on Windows:[/]");
            AnsiConsole.MarkupLine("1. Install Docker Desktop: https://www.docker.com/products/docker-desktop");
            AnsiConsole.MarkupLine("2. Enable WSL2 integration in Docker Desktop settings");
            AnsiConsole.MarkupLine("3. Ensure Docker Desktop is running");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            AnsiConsole.MarkupLine("[bold]To install Docker on Linux:[/]");
            AnsiConsole.MarkupLine("1. Run: curl -fsSL https://get.docker.com -o get-docker.sh");
            AnsiConsole.MarkupLine("2. Run: sudo sh get-docker.sh");
            AnsiConsole.MarkupLine("3. Run: sudo systemctl start docker");
            AnsiConsole.MarkupLine("4. Run: sudo usermod -aG docker $USER");
            AnsiConsole.MarkupLine("5. Log out and back in for group changes to take effect");
            
            if (AnsiConsole.Confirm("Would you like to attempt automatic installation?", false))
            {
                return await InstallDockerLinuxAsync();
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            AnsiConsole.MarkupLine("[bold]To install Docker on macOS:[/]");
            AnsiConsole.MarkupLine("1. Install Docker Desktop: https://www.docker.com/products/docker-desktop");
            AnsiConsole.MarkupLine("2. Start Docker Desktop application");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]Please install Docker and restart this application[/]");
        return false;
    }

    private async Task<bool> InstallDockerLinuxAsync()
    {
        try
        {
            await AnsiConsole.Status()
                .StartAsync("Installing Docker...", async ctx =>
                {
                    ctx.Status("Downloading Docker installation script...");
                    var psi = new ProcessStartInfo
                    {
                        FileName = "curl",
                        Arguments = "-fsSL https://get.docker.com -o /tmp/get-docker.sh",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    
                    using var downloadProcess = Process.Start(psi);
                    if (downloadProcess != null)
                    {
                        await downloadProcess.WaitForExitAsync();
                        
                        if (downloadProcess.ExitCode == 0)
                        {
                            ctx.Status("Running Docker installation...");
                            psi.FileName = "sudo";
                            psi.Arguments = "sh /tmp/get-docker.sh";
                            
                            using var installProcess = Process.Start(psi);
                            if (installProcess != null)
                            {
                                await installProcess.WaitForExitAsync();
                                
                                if (installProcess.ExitCode == 0)
                                {
                                    ctx.Status("Starting Docker service...");
                                    psi.Arguments = "systemctl start docker";
                                    using var startProcess = Process.Start(psi);
                                    if (startProcess != null) await startProcess.WaitForExitAsync();
                                    
                                    psi.Arguments = "systemctl enable docker";
                                    using var enableProcess = Process.Start(psi);
                                    if (enableProcess != null) await enableProcess.WaitForExitAsync();
                                }
                            }
                        }
                    }
                });

            if (await IsDockerAvailableAsync())
            {
                AnsiConsole.MarkupLine("[green]√[/] Docker installed successfully!");
                AnsiConsole.MarkupLine("[yellow]Note: You may need to log out and back in for group permissions[/]");
                return true;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Installation failed:[/] {Markup.Escape(ex.Message)}");
        }

        return false;
    }

    /// <summary>
    /// Display container logs in a formatted panel - CLI-specific with Spectre.Console
    /// </summary>
    public async Task DisplayContainerLogsAsync(string containerName, int tailLines = 50, string title = "Container Logs")
    {
        var status = await GetContainerStatusAsync(containerName);
        if (status == "not found")
        {
            AnsiConsole.MarkupLine($"[yellow]Container '{Markup.Escape(containerName)}' is not running[/]");
            return;
        }

        AnsiConsole.MarkupLine($"[bold]Container Status:[/] [{(status == "running" ? "green" : "yellow")}]{status}[/]");
        AnsiConsole.WriteLine();

        var logs = await GetContainerLogsAsync(containerName, tailLines);
        
        if (logs.Length > MaxLogDisplaySize)
        {
            logs = "...\n" + logs.Substring(logs.Length - MaxLogDisplaySize);
        }

        var escapedLogs = Markup.Escape(logs);

        var panel = new Panel(escapedLogs)
        {
            Header = new PanelHeader($"[bold]{title} (last {tailLines} lines)[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(status == "running" ? Color.Green : Color.Yellow)
        };
        
        AnsiConsole.Write(panel);
    }
}

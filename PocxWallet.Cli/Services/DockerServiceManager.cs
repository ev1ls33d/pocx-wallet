using System.Diagnostics;
using System.Runtime.InteropServices;
using Spectre.Console;

namespace PocxWallet.Cli.Services;

/// <summary>
/// Manages Docker containers for PoCX services
/// </summary>
public class DockerServiceManager
{
    // Maximum log size to display before truncation
    private const int MaxLogDisplaySize = 5000;
    
    // Container startup/shutdown delays
    private const int ContainerStartupDelayMs = 1000;
    private const int ContainerShutdownDelayMs = 500;

    public DockerServiceManager()
    {
    }

    /// <summary>
    /// Check if Docker is installed and running
    /// </summary>
    public async Task<bool> IsDockerAvailableAsync()
    {
        try
        {
            var result = await ExecuteCommandAsync("docker", "version");
            return result.exitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Install Docker (or guide user to install)
    /// </summary>
    public async Task<bool> EnsureDockerInstalledAsync()
    {
        if (await IsDockerAvailableAsync())
        {
            AnsiConsole.MarkupLine("[green]✓[/] Docker is installed and running");
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

    /// <summary>
    /// Attempt to install Docker on Linux
    /// </summary>
    private async Task<bool> InstallDockerLinuxAsync()
    {
        try
        {
            await AnsiConsole.Status()
                .StartAsync("Installing Docker...", async ctx =>
                {
                    ctx.Status("Downloading Docker installation script...");
                    var downloadResult = await ExecuteCommandAsync("curl", "-fsSL https://get.docker.com -o /tmp/get-docker.sh");
                    
                    if (downloadResult.exitCode == 0)
                    {
                        ctx.Status("Running Docker installation...");
                        var installResult = await ExecuteCommandAsync("sudo", "sh /tmp/get-docker.sh");
                        
                        if (installResult.exitCode == 0)
                        {
                            ctx.Status("Starting Docker service...");
                            await ExecuteCommandAsync("sudo", "systemctl start docker");
                            await ExecuteCommandAsync("sudo", "systemctl enable docker");
                        }
                    }
                });

            if (await IsDockerAvailableAsync())
            {
                AnsiConsole.MarkupLine("[green]✓[/] Docker installed successfully!");
                AnsiConsole.MarkupLine("[yellow]Note: You may need to log out and back in for group permissions[/]");
                return true;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Installation failed:[/] {ex.Message}");
        }

        return false;
    }

    /// <summary>
    /// Ensure Docker network exists
    /// </summary>
    public async Task<bool> EnsureNetworkExistsAsync(string networkName)
    {
        // Check if network exists
        var result = await ExecuteCommandAsync("docker", $"network ls -q -f name=^{networkName}$");
        
        if (string.IsNullOrWhiteSpace(result.output))
        {
            // Create network
            AnsiConsole.MarkupLine($"[bold]Creating Docker network:[/] {networkName}");
            var createResult = await ExecuteCommandAsync("docker", $"network create {networkName}");
            
            if (createResult.exitCode == 0)
            {
                AnsiConsole.MarkupLine("[green]✓[/] Network created successfully");
                return true;
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Failed to create network:[/] {createResult.output}");
                return false;
            }
        }
        
        return true;
    }

    /// <summary>
    /// Start a Docker container
    /// </summary>
    public async Task<bool> StartContainerAsync(
        string containerName,
        string imageName,
        string repository,
        string imageTag,
        Dictionary<string, string>? environmentVars = null,
        Dictionary<string, string>? volumeMounts = null,
        Dictionary<int, int>? portMappings = null,
        string? command = null,
        string? network = null,
        List<string>? readOnlyVolumes = null)
    {
        var fullImageName = $"{repository}/{imageName}:{imageTag}";
        
        // Always stop and remove existing container to ensure settings changes are applied
        AnsiConsole.MarkupLine($"[dim]Cleaning up existing container if present...[/]");
        await ExecuteCommandAsync("docker", $"stop --time=10 {containerName}", suppressOutput: true);
        await ExecuteCommandAsync("docker", $"rm {containerName}", suppressOutput: true);

        // Build docker run command
        var args = new List<string> { "run", "-dit", "--name", containerName };

        // Add network if specified
        if (!string.IsNullOrWhiteSpace(network))
        {
            args.Add("--network");
            args.Add(network);
        }

        // Add environment variables
        if (environmentVars != null)
        {
            foreach (var (key, value) in environmentVars)
            {
                args.Add("-e");
                args.Add($"{key}={value}");
            }
        }

        // Add volume mounts
        if (volumeMounts != null)
        {
            foreach (var (hostPath, containerPath) in volumeMounts)
            {
                var isReadOnly = readOnlyVolumes?.Contains(hostPath) ?? false;
                args.Add("-v");
                args.Add($"{hostPath}:{containerPath}{(isReadOnly ? ":ro" : "")}");
            }
        }

        // Add port mappings
        if (portMappings != null)
        {
            foreach (var (hostPort, containerPort) in portMappings)
            {
                args.Add("-p");
                args.Add($"{hostPort}:{containerPort}");
            }
        }

        args.Add(fullImageName);

        if (!string.IsNullOrWhiteSpace(command))
        {
            args.AddRange(command.Split(' '));
        }

        AnsiConsole.MarkupLine($"[bold]Starting container:[/] {containerName}");
        var result = await ExecuteCommandAsync("docker", string.Join(" ", args));

        if (result.exitCode == 0)
        {
            // Wait a moment for container to fully start
            await Task.Delay(ContainerStartupDelayMs);
            
            // Verify container is running
            var status = await GetContainerStatusAsync(containerName);
            if (status == "running")
            {
                AnsiConsole.MarkupLine("[green]✓[/] Container started successfully");
                return true;
            }
            else
            {
                AnsiConsole.MarkupLine($"[yellow]⚠[/] Container created but status is: {Markup.Escape(status)}");
                return false; // Return false if not running
            }
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]Failed to start container:[/] {Markup.Escape(result.output)}");
            return false;
        }
    }

    /// <summary>
    /// Stop a Docker container
    /// </summary>
    public async Task<bool> StopContainerAsync(string containerName)
    {
        ValidateContainerName(containerName);
        AnsiConsole.MarkupLine($"[bold]Stopping container:[/] {containerName}");
        
        var result = await ExecuteCommandAsync("docker", $"stop {containerName}");
        
        if (result.exitCode == 0)
        {
            // Wait a moment for container to fully stop
            await Task.Delay(ContainerShutdownDelayMs);
            AnsiConsole.MarkupLine("[green]✓[/] Container stopped successfully");
            return true;
        }
        else
        {
            AnsiConsole.MarkupLine($"[yellow]Container may not be running[/]");
            return false;
        }
    }

    /// <summary>
    /// Remove a Docker container
    /// </summary>
    public async Task<bool> RemoveContainerAsync(string containerName)
    {
        ValidateContainerName(containerName);
        AnsiConsole.MarkupLine($"[bold]Removing container:[/] {containerName}");
        
        // Stop first
        await StopContainerAsync(containerName);
        
        var result = await ExecuteCommandAsync("docker", $"rm {containerName}");
        
        if (result.exitCode == 0)
        {
            AnsiConsole.MarkupLine("[green]✓[/] Container removed successfully");
            return true;
        }
        else
        {
            AnsiConsole.MarkupLine($"[yellow]Container may not exist[/]");
            return false;
        }
    }

    /// <summary>
    /// Get container status
    /// </summary>
    public async Task<string> GetContainerStatusAsync(string containerName)
    {
        ValidateContainerName(containerName);
        var result = await ExecuteCommandAsync("docker", $"inspect -f {{{{.State.Status}}}} {containerName}");
        return result.exitCode == 0 ? result.output.Trim() : "not found";
    }

    /// <summary>
    /// Get container logs
    /// </summary>
    public async Task<string> GetContainerLogsAsync(string containerName, int tailLines = 50)
    {
        ValidateContainerName(containerName);
        if (tailLines < 1 || tailLines > 10000)
            throw new ArgumentException("Tail lines must be between 1 and 10000", nameof(tailLines));
        
        var result = await ExecuteCommandAsync("docker", $"logs --tail {tailLines} {containerName}");
        return result.exitCode == 0 ? result.output : $"Failed to get logs: {result.output}";
    }

    /// <summary>
    /// Display container logs in a formatted panel
    /// </summary>
    public async Task DisplayContainerLogsAsync(string containerName, int tailLines = 50, string title = "Container Logs")
    {
        ValidateContainerName(containerName);
        
        var status = await GetContainerStatusAsync(containerName);
        if (status == "not found")
        {
            AnsiConsole.MarkupLine($"[yellow]Container '{containerName}' is not running[/]");
            return;
        }

        AnsiConsole.MarkupLine($"[bold]Container Status:[/] [{(status == "running" ? "green" : "yellow")}]{status}[/]");
        AnsiConsole.WriteLine();

        var logs = await GetContainerLogsAsync(containerName, tailLines);
        
        // Limit log display to reasonable size
        if (logs.Length > MaxLogDisplaySize)
        {
            logs = "...\n" + logs.Substring(logs.Length - MaxLogDisplaySize);
        }

        // Escape brackets in logs to prevent markup interpretation
        var escapedLogs = Markup.Escape(logs);

        var panel = new Panel(escapedLogs)
        {
            Header = new PanelHeader($"[bold]{title} (last {tailLines} lines)[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(status == "running" ? Color.Green : Color.Yellow)
        };
        
        AnsiConsole.Write(panel);
    }

    /// <summary>
    /// Validate container name to prevent command injection
    /// </summary>
    private void ValidateContainerName(string containerName)
    {
        if (string.IsNullOrWhiteSpace(containerName))
            throw new ArgumentException("Container name cannot be empty", nameof(containerName));
        
        // Container names can only contain: [a-zA-Z0-9][a-zA-Z0-9_.-]
        if (!System.Text.RegularExpressions.Regex.IsMatch(containerName, @"^[a-zA-Z0-9][a-zA-Z0-9_.-]*$"))
            throw new ArgumentException("Invalid container name format", nameof(containerName));
    }

    /// <summary>
    /// List all PoCX-related containers
    /// </summary>
    public async Task<List<ContainerInfo>> ListContainersAsync()
    {
        var containers = new List<ContainerInfo>();
        
        var result = await ExecuteCommandAsync("docker", 
            "ps -a --filter name=pocx-node --filter name=miner --filter name=plotter --filter name=electrs " +
            "--format {{.Names}}|{{.Status}}|{{.Ports}}");

        if (result.exitCode == 0 && !string.IsNullOrWhiteSpace(result.output))
        {
            foreach (var line in result.output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split('|');
                if (parts.Length >= 2)
                {
                    containers.Add(new ContainerInfo
                    {
                        Name = parts[0].Trim(),
                        Status = parts[1].Trim(),
                        Ports = parts.Length > 2 ? parts[2].Trim() : ""
                    });
                }
            }
        }

        return containers;
    }

    /// <summary>
    /// Execute a command in a running container
    /// </summary>
    public async Task<(int exitCode, string output)> ExecInContainerAsync(
        string containerName,
        string command)
    {
        ValidateContainerName(containerName);
        return await ExecuteCommandAsync("docker", $"exec {containerName} {command}");
    }

    /// <summary>
    /// Execute a shell command
    /// </summary>
    private async Task<(int exitCode, string output)> ExecuteCommandAsync(string command, string arguments, bool suppressOutput = false)
    {
        var psi = new ProcessStartInfo
        {
            FileName = command,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var a in arguments.Split())
            psi.ArgumentList.Add(a);

        using var process = new Process { StartInfo = psi };
        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        
        await process.WaitForExitAsync();

        return (process.ExitCode, string.IsNullOrWhiteSpace(error) ? output : error);
    }

    public class ContainerInfo
    {
        public string Name { get; set; } = "";
        public string Status { get; set; } = "";
        public string Ports { get; set; } = "";
    }
}

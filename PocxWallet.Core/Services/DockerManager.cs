using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace PocxWallet.Core.Services;

/// <summary>
/// Manages Docker containers for PoCX services.
/// This is a UI-agnostic implementation that can be used by both CLI and UI.
/// </summary>
public class DockerManager
{
    // Container startup/shutdown delays
    private const int ContainerStartupDelayMs = 1000;
    private const int ContainerShutdownDelayMs = 500;
    
    private readonly IServiceLogger _logger;

    public DockerManager() : this(SilentServiceLogger.Instance)
    {
    }
    
    public DockerManager(IServiceLogger logger)
    {
        _logger = logger;
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
    /// Ensure Docker network exists
    /// </summary>
    public async Task<bool> EnsureNetworkExistsAsync(string networkName)
    {
        // Check if network exists
        var result = await ExecuteCommandAsync("docker", $"network ls -q -f name=^{networkName}$");
        
        if (string.IsNullOrWhiteSpace(result.output))
        {
            // Create network
            _logger.LogInfo($"Creating Docker network: {networkName}");
            var createResult = await ExecuteCommandAsync("docker", $"network create {networkName}");
            
            if (createResult.exitCode == 0)
            {
                _logger.LogSuccess("Network created successfully");
                return true;
            }
            else
            {
                _logger.LogError($"Failed to create network: {createResult.output}");
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
        List<string>? readOnlyVolumes = null,
        bool gpuPassthrough = false)
    {
        var fullImageName = !string.IsNullOrEmpty(repository) 
            ? $"{repository}/{imageName}:{imageTag}"
            : $"{imageName}:{imageTag}";
        
        // Always stop and remove existing container to ensure settings changes are applied
        _logger.LogDebug("Cleaning up existing container if present...");
        await ExecuteCommandAsync("docker", new[] { "stop", "--time=10", containerName }, suppressOutput: true);
        await ExecuteCommandAsync("docker", new[] { "rm", containerName }, suppressOutput: true);

        // Build docker run command using ArgumentList for proper escaping
        var args = new List<string> { "run", "-dit", "--name", containerName };

        // Add GPU passthrough if enabled (requires nvidia-docker runtime)
        if (gpuPassthrough)
        {
            args.Add("--gpus");
            args.Add("all");
        }

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

        // Add the full image name as a single argument (important for proper escaping)
        args.Add(fullImageName);

        // Add command arguments if specified
        if (!string.IsNullOrWhiteSpace(command))
        {
            // Split command into individual arguments, respecting quoted strings
            args.AddRange(SplitCommandLineArguments(command));
        }

        _logger.LogInfo($"Starting container: {containerName}");
        _logger.LogDebug($"Image: {fullImageName}");
        
        // Use the array-based overload for proper argument handling
        var result = await ExecuteCommandAsync("docker", args.ToArray());

        if (result.exitCode == 0)
        {
            // Wait a moment for container to fully start
            await Task.Delay(ContainerStartupDelayMs);
            
            // Verify container is running
            var status = await GetContainerStatusAsync(containerName);
            if (status == "running")
            {
                _logger.LogSuccess("Container started successfully");
                return true;
            }
            else
            {
                _logger.LogWarning($"Container created but status is: {status}");
                return false;
            }
        }
        else
        {
            _logger.LogError($"Failed to start container: {result.output}");
            return false;
        }
    }

    /// <summary>
    /// Stop a Docker container
    /// </summary>
    public async Task<bool> StopContainerAsync(string containerName)
    {
        ValidateContainerName(containerName);
        _logger.LogInfo($"Stopping container: {containerName}");
        
        var result = await ExecuteCommandAsync("docker", $"stop {containerName}");
        
        if (result.exitCode == 0)
        {
            // Wait a moment for container to fully stop
            await Task.Delay(ContainerShutdownDelayMs);
            _logger.LogSuccess("Container stopped successfully");
            return true;
        }
        else
        {
            _logger.LogWarning("Container may not be running");
            return false;
        }
    }

    /// <summary>
    /// Remove a Docker container
    /// </summary>
    public async Task<bool> RemoveContainerAsync(string containerName)
    {
        ValidateContainerName(containerName);
        _logger.LogInfo($"Removing container: {containerName}");
        
        // Stop first
        await StopContainerAsync(containerName);
        
        var result = await ExecuteCommandAsync("docker", $"rm {containerName}");
        
        if (result.exitCode == 0)
        {
            _logger.LogSuccess("Container removed successfully");
            return true;
        }
        else
        {
            _logger.LogWarning("Container may not exist");
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
        // Split command but respect quoted strings to preserve JSON arguments with spaces
        var arguments = SplitCommandLineArguments(command);
        var fullArgs = new string[] { "exec", containerName }.Concat(arguments).ToArray();
        return await ExecuteCommandAsync("docker", fullArgs);
    }

    /// <summary>
    /// Pull a Docker image
    /// </summary>
    public async Task<bool> PullImageAsync(string fullImageName)
    {
        _logger.LogInfo($"Pulling Docker image: {fullImageName}");
        
        var result = await ExecuteCommandAsync("docker", new[] { "pull", fullImageName });
        
        if (result.exitCode == 0)
        {
            _logger.LogSuccess("Image pulled successfully");
            return true;
        }
        else
        {
            _logger.LogError($"Failed to pull image: {result.output}");
            return false;
        }
    }

    /// <summary>
    /// Validate container name to prevent command injection
    /// </summary>
    private void ValidateContainerName(string containerName)
    {
        if (string.IsNullOrWhiteSpace(containerName))
            throw new ArgumentException("Container name cannot be empty", nameof(containerName));
        
        // Container names can only contain: [a-zA-Z0-9][a-zA-Z0-9_.-]
        if (!Regex.IsMatch(containerName, @"^[a-zA-Z0-9][a-zA-Z0-9_.-]*$"))
            throw new ArgumentException("Invalid container name format", nameof(containerName));
    }

    /// <summary>
    /// Split command line arguments while respecting quoted strings
    /// </summary>
    public static string[] SplitCommandLineArguments(string commandLine)
    {
        var args = new List<string>();
        var inSingleQuote = false;
        var inDoubleQuote = false;
        var current = new StringBuilder();
        var escapeNext = false;

        foreach (char c in commandLine)
        {
            if (escapeNext)
            {
                current.Append(c);
                escapeNext = false;
                continue;
            }

            if (c == '\\')
            {
                escapeNext = true;
                continue;
            }

            if (c == '\'' && !inDoubleQuote)
            {
                inSingleQuote = !inSingleQuote;
            }
            else if (c == '"' && !inSingleQuote)
            {
                inDoubleQuote = !inDoubleQuote;
            }
            else if (c == ' ' && !inSingleQuote && !inDoubleQuote)
            {
                if (current.Length > 0)
                {
                    args.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
        {
            args.Add(current.ToString());
        }

        return args.ToArray();
    }

    /// <summary>
    /// Execute a shell command with string[] arguments
    /// </summary>
    private async Task<(int exitCode, string output)> ExecuteCommandAsync(string command, string[] arguments, bool suppressOutput = false)
    {
        var psi = new ProcessStartInfo
        {
            FileName = command,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in arguments)
            psi.ArgumentList.Add(arg);

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
    /// Execute a shell command (splits by space)
    /// </summary>
    private async Task<(int exitCode, string output)> ExecuteCommandAsync(string command, string arguments, bool suppressOutput = false)
    {
        return await ExecuteCommandAsync(command, arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries), suppressOutput);
    }
}

/// <summary>
/// Information about a Docker container
/// </summary>
public class ContainerInfo
{
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
    public string Ports { get; set; } = "";
}

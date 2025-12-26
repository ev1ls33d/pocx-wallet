using PocxWallet.Cli.Services;

namespace PocxWallet.UI.Services;

/// <summary>
/// Implementation of IDockerService using DockerServiceManager from CLI
/// </summary>
public class DockerService : IDockerService
{
    private readonly DockerServiceManager _dockerManager;

    public DockerService()
    {
        _dockerManager = new DockerServiceManager();
    }

    public async Task<bool> IsServiceRunningAsync(string serviceId)
    {
        // Map service ID to container name
        var containerName = GetContainerName(serviceId);
        
        var status = await _dockerManager.GetContainerStatusAsync(containerName);
        return status.Equals("running", StringComparison.OrdinalIgnoreCase);
    }

    public Task<bool> StartServiceAsync(string serviceId)
    {
        // TODO: Implement service start with proper configuration from services.yaml
        // This requires integration with ServiceDefinitionLoader and command building
        // For now, return false as it needs complex setup
        return Task.FromResult(false);
    }

    public async Task<bool> StopServiceAsync(string serviceId)
    {
        var containerName = GetContainerName(serviceId);
        return await _dockerManager.StopContainerAsync(containerName);
    }

    public async Task<string> GetServiceLogsAsync(string serviceId, int tailLines = 100)
    {
        var containerName = GetContainerName(serviceId);
        return await _dockerManager.GetContainerLogsAsync(containerName, tailLines);
    }

    public async Task<(int exitCode, string output)> ExecuteInContainerAsync(string containerName, string command)
    {
        return await _dockerManager.ExecInContainerAsync(containerName, command);
    }

    private string GetContainerName(string serviceId)
    {
        // Map service IDs to container names based on services.yaml defaults
        return serviceId switch
        {
            "bitcoin-node" or "node" => "pocx-node",
            "plotter" => "pocx-plotter",
            "miner" => "pocx-miner",
            "aggregator" => "pocx-aggregator",
            "electrs" => "pocx-electrs",
            _ => $"pocx-{serviceId}"
        };
    }
}

namespace PocxWallet.UI.Services;

/// <summary>
/// Stub implementation of IDockerService for initial UI development
/// </summary>
public class DockerService : IDockerService
{
    public Task<bool> IsServiceRunningAsync(string serviceId)
    {
        // TODO: Integrate with DockerServiceManager from CLI
        return Task.FromResult(false);
    }

    public Task<bool> StartServiceAsync(string serviceId)
    {
        // TODO: Integrate with DockerServiceManager from CLI
        return Task.FromResult(false);
    }

    public Task<bool> StopServiceAsync(string serviceId)
    {
        // TODO: Integrate with DockerServiceManager from CLI
        return Task.FromResult(false);
    }

    public Task<string> GetServiceLogsAsync(string serviceId, int tailLines = 100)
    {
        // TODO: Integrate with DockerServiceManager from CLI
        return Task.FromResult("Logs not available yet...");
    }

    public Task<(int exitCode, string output)> ExecuteInContainerAsync(string containerName, string command)
    {
        // TODO: Integrate with DockerServiceManager from CLI
        return Task.FromResult((0, ""));
    }
}

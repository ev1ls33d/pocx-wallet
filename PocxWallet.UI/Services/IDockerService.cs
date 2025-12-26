namespace PocxWallet.UI.Services;

/// <summary>
/// Service interface for Docker operations
/// </summary>
public interface IDockerService
{
    /// <summary>
    /// Checks if a service is running
    /// </summary>
    Task<bool> IsServiceRunningAsync(string serviceId);
    
    /// <summary>
    /// Starts a service
    /// </summary>
    Task<bool> StartServiceAsync(string serviceId);
    
    /// <summary>
    /// Stops a service
    /// </summary>
    Task<bool> StopServiceAsync(string serviceId);
    
    /// <summary>
    /// Gets the logs for a service
    /// </summary>
    Task<string> GetServiceLogsAsync(string serviceId, int tailLines = 100);
    
    /// <summary>
    /// Executes a command in a container
    /// </summary>
    Task<(int exitCode, string output)> ExecuteInContainerAsync(string containerName, string command);
}

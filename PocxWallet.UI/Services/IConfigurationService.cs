using CliConfig = PocxWallet.Cli.Configuration;

namespace PocxWallet.UI.Services;

/// <summary>
/// Service interface for configuration operations
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Gets the service configuration from services.yaml
    /// </summary>
    CliConfig.ServiceConfiguration GetServiceConfiguration();
    
    /// <summary>
    /// Gets a specific service definition
    /// </summary>
    CliConfig.ServiceDefinition? GetServiceDefinition(string serviceId);
    
    /// <summary>
    /// Saves the service configuration
    /// </summary>
    void SaveServiceConfiguration(CliConfig.ServiceConfiguration configuration);
}

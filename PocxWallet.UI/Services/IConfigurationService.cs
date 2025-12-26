using PocxWallet.UI.Models;

namespace PocxWallet.UI.Services;

/// <summary>
/// Service interface for configuration operations
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Gets the service configuration from services.yaml
    /// </summary>
    ServiceConfiguration GetServiceConfiguration();
    
    /// <summary>
    /// Gets a specific service definition
    /// </summary>
    ServiceDefinition? GetServiceDefinition(string serviceId);
    
    /// <summary>
    /// Saves the service configuration
    /// </summary>
    void SaveServiceConfiguration(ServiceConfiguration configuration);
}

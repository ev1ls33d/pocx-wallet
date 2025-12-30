using PocxWallet.Core.Services;

namespace PocxWallet.UI.Services;

/// <summary>
/// Service interface for configuration operations
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Get the full service configuration
    /// </summary>
    ServiceConfiguration GetServiceConfiguration();
    
    /// <summary>
    /// Get a specific service definition by ID
    /// </summary>
    ServiceDefinition? GetServiceDefinition(string serviceId);
    
    /// <summary>
    /// Save the service configuration
    /// </summary>
    void SaveServiceConfiguration(ServiceConfiguration configuration);
    
    /// <summary>
    /// Save current service definitions
    /// </summary>
    void SaveServiceDefinitions();
    
    /// <summary>
    /// Get IDs of all enabled services
    /// </summary>
    IEnumerable<string> GetEnabledServiceIds();
}

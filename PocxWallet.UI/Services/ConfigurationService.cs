using PocxWallet.Cli.Configuration;

namespace PocxWallet.UI.Services;

/// <summary>
/// Implementation of IConfigurationService using ServiceDefinitionLoader from CLI
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private ServiceConfiguration? _configuration;

    public ServiceConfiguration GetServiceConfiguration()
    {
        // Load from services.yaml using CLI loader
        _configuration ??= ServiceDefinitionLoader.LoadServices();
        return _configuration;
    }

    public ServiceDefinition? GetServiceDefinition(string serviceId)
    {
        var config = GetServiceConfiguration();
        return config.Services?.FirstOrDefault(s => 
            s.Id?.Equals(serviceId, StringComparison.OrdinalIgnoreCase) == true);
    }

    public void SaveServiceConfiguration(ServiceConfiguration configuration)
    {
        _configuration = configuration;
        // TODO: Save to services.yaml
        // This would require implementing YAML serialization
    }
}

using PocxWallet.UI.Models;

namespace PocxWallet.UI.Services;

/// <summary>
/// Implementation of IConfigurationService
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private ServiceConfiguration? _configuration;

    public ServiceConfiguration GetServiceConfiguration()
    {
        // TODO: Load from services.yaml
        _configuration ??= new ServiceConfiguration { Services = new List<ServiceDefinition>() };
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
    }
}

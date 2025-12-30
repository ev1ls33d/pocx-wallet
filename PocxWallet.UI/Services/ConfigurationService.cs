using PocxWallet.Core.Services;
using PocxWallet.Cli.Configuration;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PocxWallet.UI.Services;

/// <summary>
/// Implementation of IConfigurationService using Core ServiceConfigurationLoader
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly ServiceConfigurationLoader _loader;
    private ServiceConfiguration? _configuration;

    public ConfigurationService()
    {
        _loader = new ServiceConfigurationLoader();
    }

    public ServiceConfiguration GetServiceConfiguration()
    {
        return _configuration ??= _loader.LoadServices() ?? new ServiceConfiguration();
    }

    public ServiceDefinition? GetServiceDefinition(string serviceId)
    {
        var config = GetServiceConfiguration();
        return ServiceConfigurationHelper.GetServiceById(config, serviceId);
    }

    public void SaveServiceConfiguration(ServiceConfiguration configuration)
    {
        _configuration = configuration;
        SaveServiceDefinitions();
    }

    public void SaveServiceDefinitions()
    {
        if (_configuration == null) return;
        _loader.SaveServices(_configuration);
    }

    public IEnumerable<string> GetEnabledServiceIds()
    {
        var config = GetServiceConfiguration();
        return ServiceConfigurationHelper.GetEnabledServices(config)
            .Where(s => !string.IsNullOrEmpty(s.Id))
            .Select(s => s.Id!);
    }
}

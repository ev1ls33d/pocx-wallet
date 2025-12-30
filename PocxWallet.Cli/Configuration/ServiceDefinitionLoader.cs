using PocxWallet.Core.Services;
using PocxWallet.Cli.Services;
using Spectre.Console;

namespace PocxWallet.Cli.Configuration;

/// <summary>
/// CLI-specific service definition loader with Spectre.Console output
/// </summary>
public static class ServiceDefinitionLoader
{
    private static readonly ServiceConfigurationLoader _coreLoader = 
        new(Services.SpectreServiceLogger.Instance);

    /// <summary>
    /// Load service configuration from the default services.yaml file
    /// </summary>
    public static ServiceConfiguration? LoadServices()
    {
        return _coreLoader.LoadServices();
    }

    /// <summary>
    /// Load service configuration from a specified path
    /// </summary>
    public static ServiceConfiguration? LoadServices(string servicesPath)
    {
        return _coreLoader.LoadServices(servicesPath);
    }

    /// <summary>
    /// Save service configuration to the default services.yaml file
    /// </summary>
    public static void SaveServices(ServiceConfiguration config)
    {
        _coreLoader.SaveServices(config);
    }

    /// <summary>
    /// Save service configuration to a specified path
    /// </summary>
    public static void SaveServices(ServiceConfiguration config, string servicesPath)
    {
        _coreLoader.SaveServices(config, servicesPath);
    }

    /// <summary>
    /// Get enabled services sorted by menu order
    /// </summary>
    public static List<ServiceDefinition> GetEnabledServices(ServiceConfiguration? config)
    {
        return ServiceConfigurationHelper.GetEnabledServices(config);
    }

    /// <summary>
    /// Get service by ID
    /// </summary>
    public static ServiceDefinition? GetServiceById(ServiceConfiguration? config, string serviceId)
    {
        return ServiceConfigurationHelper.GetServiceById(config, serviceId);
    }
}

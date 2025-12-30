using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PocxWallet.Core.Services;

/// <summary>
/// Loads and saves service configuration from services.yaml
/// </summary>
public class ServiceConfigurationLoader : IServiceConfigurationLoader
{
    private const string DefaultServicesPath = "services.yaml";
    private readonly IServiceLogger _logger;

    public ServiceConfigurationLoader() : this(SilentServiceLogger.Instance)
    {
    }
    
    public ServiceConfigurationLoader(IServiceLogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Load service configuration from the default services.yaml file
    /// </summary>
    public ServiceConfiguration? LoadServices()
    {
        return LoadServices(DefaultServicesPath);
    }

    /// <summary>
    /// Load service configuration from a specified path
    /// </summary>
    public ServiceConfiguration? LoadServices(string servicesPath)
    {
        if (!File.Exists(servicesPath))
        {
            _logger.LogDebug($"Note: services.yaml not found at '{servicesPath}'. Dynamic services will not be available.");
            return null;
        }

        try
        {
            var yaml = File.ReadAllText(servicesPath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            var config = deserializer.Deserialize<ServiceConfiguration>(yaml);
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Warning: Failed to load services.yaml: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Save service configuration to the default services.yaml file
    /// </summary>
    public void SaveServices(ServiceConfiguration config)
    {
        SaveServices(config, DefaultServicesPath);
    }

    /// <summary>
    /// Save service configuration to a specified path
    /// </summary>
    public void SaveServices(ServiceConfiguration config, string servicesPath)
    {
        try
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
                .Build();

            var yaml = serializer.Serialize(config);
            File.WriteAllText(servicesPath, yaml);
            _logger.LogDebug("Settings saved to services.yaml");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error saving services.yaml: {ex.Message}");
        }
    }
}

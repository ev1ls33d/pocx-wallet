using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Spectre.Console;

namespace PocxWallet.Cli.Configuration;

/// <summary>
/// Loads and parses service definitions from services.yaml
/// </summary>
public static class ServiceDefinitionLoader
{
    private const string DefaultServicesPath = "services.yaml";

    /// <summary>
    /// Load service configuration from the default services.yaml file
    /// </summary>
    public static ServiceConfiguration? LoadServices()
    {
        return LoadServices(DefaultServicesPath);
    }

    /// <summary>
    /// Load service configuration from a specified path
    /// </summary>
    public static ServiceConfiguration? LoadServices(string servicesPath)
    {
        if (!File.Exists(servicesPath))
        {
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
            AnsiConsole.MarkupLine($"[yellow]Warning: Failed to load services.yaml: {Markup.Escape(ex.Message)}[/]");
            return null;
        }
    }

    /// <summary>
    /// Get enabled services sorted by menu order
    /// </summary>
    public static List<ServiceDefinition> GetEnabledServices(ServiceConfiguration? config)
    {
        if (config?.Services == null)
        {
            return new List<ServiceDefinition>();
        }

        return config.Services
            .Where(s => s.Enabled)
            .OrderBy(s => s.Menu?.MainMenuOrder ?? int.MaxValue)
            .ToList();
    }

    /// <summary>
    /// Get service by ID
    /// </summary>
    public static ServiceDefinition? GetServiceById(ServiceConfiguration? config, string serviceId)
    {
        return config?.Services?.FirstOrDefault(s => s.Id == serviceId);
    }
}

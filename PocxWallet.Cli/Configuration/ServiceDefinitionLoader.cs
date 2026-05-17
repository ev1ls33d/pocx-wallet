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
        // Try provided path first
        string effectivePath = servicesPath;
        
        if (!File.Exists(effectivePath))
        {
            // Try relative to entry assembly / base directory
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            effectivePath = Path.Combine(baseDir, servicesPath);
        }

        if (!File.Exists(effectivePath))
        {
            // Try one level up (common in dev/debug)
            effectivePath = Path.Combine("..", servicesPath);
        }

        if (!File.Exists(effectivePath))
        {
            AnsiConsole.MarkupLine($"[dim]Note: {servicesPath} not found. Dynamic services will not be available.[/]");
            return null;
        }

        try
        {
            var yaml = File.ReadAllText(effectivePath);
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
    /// Save service configuration to the default services.yaml file
    /// </summary>
    public static void SaveServices(ServiceConfiguration config)
    {
        SaveServices(config, DefaultServicesPath);
    }

    /// <summary>
    /// Save service configuration to a specified path
    /// </summary>
    public static void SaveServices(ServiceConfiguration config, string servicesPath)
    {
        try
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
                .Build();

            var yaml = serializer.Serialize(config);
            File.WriteAllText(servicesPath, yaml);
            AnsiConsole.MarkupLine($"[dim]Settings saved to services.yaml[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error saving services.yaml: {Markup.Escape(ex.Message)}[/]");
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

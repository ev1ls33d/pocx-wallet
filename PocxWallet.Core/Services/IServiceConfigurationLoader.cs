namespace PocxWallet.Core.Services;

/// <summary>
/// Interface for loading and saving service configuration.
/// Different implementations can use different serialization formats.
/// </summary>
public interface IServiceConfigurationLoader
{
    /// <summary>
    /// Load service configuration from the default location
    /// </summary>
    ServiceConfiguration? LoadServices();
    
    /// <summary>
    /// Load service configuration from a specified path
    /// </summary>
    ServiceConfiguration? LoadServices(string path);
    
    /// <summary>
    /// Save service configuration to the default location
    /// </summary>
    void SaveServices(ServiceConfiguration config);
    
    /// <summary>
    /// Save service configuration to a specified path
    /// </summary>
    void SaveServices(ServiceConfiguration config, string path);
}

/// <summary>
/// Helper methods for working with service configurations
/// </summary>
public static class ServiceConfigurationHelper
{
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
    
    /// <summary>
    /// Build command string from service parameters
    /// </summary>
    public static string? BuildCommand(ServiceDefinition service)
    {
        var commands = new List<string>();
        
        // Add the binary executable first
        if (!string.IsNullOrEmpty(service.Container?.Binary))
        {
            commands.Add(service.Container.Binary);
        }
        
        // Add the base command if specified
        if (!string.IsNullOrEmpty(service.Container?.Command))
        {
            commands.Add(service.Container.Command);
        }

        // Add user-set parameters
        if (service.Parameters != null)
        {
            foreach (var param in service.Parameters.Where(p => !p.Hidden && p.HasUserValue))
            {
                var cliFlag = param.CliFlag;
                if (string.IsNullOrEmpty(cliFlag)) continue;

                var useEquals = param.UseEquals ?? false;

                switch (param.Type.ToLower())
                {
                    case "bool":
                        var boolValue = param.Value?.ToString()?.ToLower() == "true";
                        if (boolValue)
                        {
                            commands.Add(cliFlag);
                        }
                        break;
                    
                    case "int":
                        var intValue = param.Value?.ToString();
                        if (!string.IsNullOrEmpty(intValue))
                        {
                            commands.Add(useEquals ? $"{cliFlag}={intValue}" : $"{cliFlag} {intValue}");
                        }
                        break;
                    
                    case "string[]":
                        if (param.Value is IEnumerable<object> enumerable)
                        {
                            foreach (var item in enumerable)
                            {
                                var itemStr = item?.ToString();
                                if (!string.IsNullOrEmpty(itemStr))
                                {
                                    commands.Add(useEquals ? $"{cliFlag}={itemStr}" : $"{cliFlag} {itemStr}");
                                }
                            }
                        }
                        else if (param.Value is string strArray)
                        {
                            foreach (var item in strArray.Split(',', StringSplitOptions.RemoveEmptyEntries))
                            {
                                commands.Add(useEquals ? $"{cliFlag}={item.Trim()}" : $"{cliFlag} {item.Trim()}");
                            }
                        }
                        break;
                    
                    default:
                        var strValue = param.Value?.ToString();
                        if (!string.IsNullOrEmpty(strValue))
                        {
                            commands.Add(useEquals ? $"{cliFlag}={strValue}" : $"{cliFlag} {strValue}");
                        }
                        break;
                }
            }
        }

        return commands.Count > 0 ? string.Join(" ", commands) : null;
    }
    
    /// <summary>
    /// Build environment variables dictionary from service definition
    /// </summary>
    public static Dictionary<string, string> BuildEnvironmentVariables(ServiceDefinition service)
    {
        var envVars = new Dictionary<string, string>();
        
        if (service.Environment == null) return envVars;

        foreach (var envVar in service.Environment)
        {
            if (!string.IsNullOrEmpty(envVar.Name))
            {
                var value = envVar.ValueOverride ?? envVar.Value;
                if (!string.IsNullOrEmpty(value))
                {
                    envVars[envVar.Name] = value;
                }
            }
        }

        return envVars;
    }
    
    /// <summary>
    /// Build volume mappings dictionary from service definition
    /// </summary>
    public static Dictionary<string, string> BuildVolumeMappings(ServiceDefinition service)
    {
        var mappings = new Dictionary<string, string>();
        
        if (service.Volumes == null) return mappings;

        foreach (var volume in service.Volumes)
        {
            var hostPath = volume.HostPathOverride ?? volume.HostPathDefault;
            if (!string.IsNullOrEmpty(hostPath))
            {
                mappings[hostPath] = volume.ContainerPath;
            }
        }

        return mappings;
    }
    
    /// <summary>
    /// Build port mappings dictionary from service definition
    /// </summary>
    public static Dictionary<int, int> BuildPortMappings(ServiceDefinition service)
    {
        var mappings = new Dictionary<int, int>();
        
        if (service.Ports == null) return mappings;

        foreach (var port in service.Ports)
        {
            if (port.Optional) continue;
            
            var hostPort = port.HostPortOverride ?? port.HostPortDefault ?? port.ContainerPort;
            mappings[hostPort] = port.ContainerPort;
        }

        return mappings;
    }
    
    /// <summary>
    /// Get list of read-only volume paths
    /// </summary>
    public static List<string> GetReadOnlyVolumes(ServiceDefinition service)
    {
        var readOnlyPaths = new List<string>();
        
        if (service.Volumes == null) return readOnlyPaths;

        foreach (var volume in service.Volumes.Where(v => v.ReadOnly))
        {
            var hostPath = volume.HostPathOverride ?? volume.HostPathDefault;
            if (!string.IsNullOrEmpty(hostPath))
            {
                readOnlyPaths.Add(hostPath);
            }
        }

        return readOnlyPaths;
    }
}

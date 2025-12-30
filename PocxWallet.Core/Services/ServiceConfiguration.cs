using YamlDotNet.Serialization;

namespace PocxWallet.Core.Services;

/// <summary>
/// Execution mode for a service
/// </summary>
public enum ExecutionMode
{
    /// <summary>
    /// Service runs in a Docker container (default)
    /// </summary>
    Docker,
    
    /// <summary>
    /// Service runs as a native process on the host
    /// </summary>
    Native
}

/// <summary>
/// Root model for the services.yaml configuration file
/// </summary>
public class ServiceConfiguration
{
    [YamlMember(Alias = "version")]
    public string Version { get; set; } = "1.0";

    [YamlMember(Alias = "defaults")]
    public ServiceDefaults? Defaults { get; set; }

    [YamlMember(Alias = "services")]
    public List<ServiceDefinition> Services { get; set; } = new();

    [YamlMember(Alias = "categories")]
    public List<CategoryDefinition>? Categories { get; set; }

    [YamlMember(Alias = "parameter_categories")]
    public List<ParameterCategoryDefinition>? ParameterCategories { get; set; }
}

/// <summary>
/// Default settings applied to all services
/// </summary>
public class ServiceDefaults
{
    [YamlMember(Alias = "docker_network")]
    public string DockerNetwork { get; set; } = "pocx";

    [YamlMember(Alias = "restart_policy")]
    public string RestartPolicy { get; set; } = "unless-stopped";

    [YamlMember(Alias = "log_driver")]
    public string LogDriver { get; set; } = "json-file";

    [YamlMember(Alias = "log_max_size")]
    public string LogMaxSize { get; set; } = "10m";

    [YamlMember(Alias = "log_max_files")]
    public int LogMaxFiles { get; set; } = 3;
}

/// <summary>
/// Individual service definition from services.yaml
/// </summary>
public class ServiceDefinition
{
    [YamlMember(Alias = "id")]
    public string Id { get; set; } = "";

    [YamlMember(Alias = "name")]
    public string Name { get; set; } = "";

    [YamlMember(Alias = "description")]
    public string Description { get; set; } = "";

    [YamlMember(Alias = "category")]
    public string Category { get; set; } = "";

    [YamlMember(Alias = "menu_label")]
    public string MenuLabel { get; set; } = "";

    [YamlMember(Alias = "documentation_url")]
    public string? DocumentationUrl { get; set; }

    [YamlMember(Alias = "enabled")]
    public bool Enabled { get; set; } = true;

    [YamlMember(Alias = "execution_mode")]
    public string ExecutionModeString { get; set; } = "docker";

    [YamlMember(Alias = "container")]
    public ContainerConfig? Container { get; set; }

    [YamlMember(Alias = "source")]
    public ServiceSource? Source { get; set; }

    [YamlMember(Alias = "ports")]
    public List<PortMapping>? Ports { get; set; }

    [YamlMember(Alias = "volumes")]
    public List<VolumeMapping>? Volumes { get; set; }

    [YamlMember(Alias = "environment")]
    public List<EnvironmentVariable>? Environment { get; set; }

    [YamlMember(Alias = "parameters")]
    public List<ServiceParameter>? Parameters { get; set; }

    [YamlMember(Alias = "config_file")]
    public ConfigFileDefinition? ConfigFile { get; set; }

    [YamlMember(Alias = "depends_on")]
    public List<ServiceDependency>? DependsOn { get; set; }

    [YamlMember(Alias = "health_check")]
    public HealthCheckConfig? HealthCheck { get; set; }

    [YamlMember(Alias = "menu")]
    public MenuConfig? Menu { get; set; }

    [YamlMember(Alias = "settings")]
    public List<ServiceSetting>? Settings { get; set; }

    [YamlMember(Alias = "container_name_override")]
    public string? ContainerNameOverride { get; set; }

    [YamlMember(Alias = "network_override")]
    public string? NetworkOverride { get; set; }

    [YamlMember(Alias = "spawn_new_console")]
    public bool SpawnNewConsole { get; set; } = true;
    
    /// <summary>
    /// Get the execution mode for this service
    /// </summary>
    public ExecutionMode GetExecutionMode()
    {
        return ExecutionModeString?.ToLower() switch
        {
            "native" => ExecutionMode.Native,
            _ => ExecutionMode.Docker
        };
    }

    /// <summary>
    /// Get the container name for this service
    /// </summary>
    public string GetContainerName()
    {
        return ContainerNameOverride 
            ?? Container?.ContainerNameDefault 
            ?? Container?.ContainerNameSetting 
            ?? $"pocx-{Id}";
    }
    
    /// <summary>
    /// Get the full Docker image reference (repository/image:tag)
    /// </summary>
    public string GetFullDockerImage()
    {
        var repository = GetDockerRepository();
        var image = GetDockerImageName();
        var tag = GetDockerTag();
        
        if (!string.IsNullOrEmpty(repository))
        {
            return $"{repository}/{image}:{tag}";
        }
        return $"{image}:{tag}";
    }
    
    /// <summary>
    /// Get the Docker repository (e.g., "ghcr.io/ev1ls33d/pocx-wallet")
    /// </summary>
    public string GetDockerRepository()
    {
        if (!string.IsNullOrEmpty(Container?.Repository))
        {
            return Container.Repository;
        }
        return "ghcr.io/ev1ls33d/pocx-wallet";
    }
    
    /// <summary>
    /// Get the Docker image name (e.g., "bitcoin", "electrs")
    /// </summary>
    public string GetDockerImageName()
    {
        return Container?.Image ?? Id;
    }
    
    /// <summary>
    /// Get the Docker image tag (e.g., "latest", "v1.0.0")
    /// </summary>
    public string GetDockerTag()
    {
        return Container?.DefaultTag ?? "latest";
    }
    
    /// <summary>
    /// Update the Docker image configuration
    /// </summary>
    public void SetDockerImage(string repository, string image, string tag)
    {
        Container ??= new ContainerConfig();
        Container.Repository = repository;
        Container.Image = image;
        Container.DefaultTag = tag;
    }
}

/// <summary>
/// Container configuration for a service
/// </summary>
public class ContainerConfig
{
    [YamlMember(Alias = "image")]
    public string Image { get; set; } = "";

    [YamlMember(Alias = "repository")]
    public string Repository { get; set; } = "";

    [YamlMember(Alias = "default_tag")]
    public string DefaultTag { get; set; } = "latest";

    [YamlMember(Alias = "container_name_setting")]
    public string? ContainerNameSetting { get; set; }

    [YamlMember(Alias = "container_name_default")]
    public string? ContainerNameDefault { get; set; }

    [YamlMember(Alias = "working_dir")]
    public string? WorkingDir { get; set; }

    [YamlMember(Alias = "entrypoint")]
    public string? Entrypoint { get; set; }

    [YamlMember(Alias = "command")]
    public string? Command { get; set; }

    [YamlMember(Alias = "binary")]
    public string? Binary { get; set; }
}

/// <summary>
/// Service source configuration
/// </summary>
public class ServiceSource
{
    [YamlMember(Alias = "docker")]
    public DockerSource? Docker { get; set; }

    [YamlMember(Alias = "native")]
    public NativeSource? Native { get; set; }

    [YamlMember(Alias = "repository")]
    public string? Repository { get; set; }

    [YamlMember(Alias = "branch")]
    public string? Branch { get; set; }

    [YamlMember(Alias = "build_command")]
    public string? BuildCommand { get; set; }

    [YamlMember(Alias = "binary_paths")]
    public List<string>? BinaryPaths { get; set; }
}

/// <summary>
/// Dynamic source configuration for runtime discovery
/// </summary>
public class DynamicSourceConfig
{
    [YamlMember(Alias = "repository")]
    public string Repository { get; set; } = "";

    [YamlMember(Alias = "filter")]
    public string Filter { get; set; } = "";

    [YamlMember(Alias = "whitelist")]
    public List<string>? Whitelist { get; set; }
}

/// <summary>
/// Docker source configuration
/// </summary>
public class DockerSource
{
    [YamlMember(Alias = "dynamic")]
    public DynamicSourceConfig? Dynamic { get; set; }

    [YamlMember(Alias = "images")]
    public List<DockerImage>? Images { get; set; }
}

/// <summary>
/// Docker image definition
/// </summary>
public class DockerImage
{
    [YamlMember(Alias = "repository")]
    public string Repository { get; set; } = "";

    [YamlMember(Alias = "image")]
    public string Image { get; set; } = "";

    [YamlMember(Alias = "tag")]
    public string Tag { get; set; } = "";

    [YamlMember(Alias = "description")]
    public string? Description { get; set; }
}

/// <summary>
/// Native source configuration
/// </summary>
public class NativeSource
{
    [YamlMember(Alias = "dynamic")]
    public DynamicSourceConfig? Dynamic { get; set; }

    [YamlMember(Alias = "downloads")]
    public List<NativeDownload>? Downloads { get; set; }
}

/// <summary>
/// Native binary download definition
/// </summary>
public class NativeDownload
{
    [YamlMember(Alias = "url")]
    public string Url { get; set; } = "";

    [YamlMember(Alias = "version")]
    public string Version { get; set; } = "";

    [YamlMember(Alias = "platform")]
    public string Platform { get; set; } = "";

    [YamlMember(Alias = "description")]
    public string? Description { get; set; }

    [YamlMember(Alias = "whitelist")]
    public List<string>? Whitelist { get; set; }
}

/// <summary>
/// Port mapping configuration
/// </summary>
public class PortMapping
{
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = "";

    [YamlMember(Alias = "container_port")]
    public int ContainerPort { get; set; }

    [YamlMember(Alias = "host_port_setting")]
    public string? HostPortSetting { get; set; }

    [YamlMember(Alias = "host_port_default")]
    public int? HostPortDefault { get; set; }

    [YamlMember(Alias = "description")]
    public string Description { get; set; } = "";

    [YamlMember(Alias = "protocol")]
    public string Protocol { get; set; } = "tcp";

    [YamlMember(Alias = "optional")]
    public bool Optional { get; set; }

    [YamlMember(Alias = "host_port_override")]
    public int? HostPortOverride { get; set; }
}

/// <summary>
/// Volume mapping configuration
/// </summary>
public class VolumeMapping
{
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = "";

    [YamlMember(Alias = "host_path_setting")]
    public string? HostPathSetting { get; set; }

    [YamlMember(Alias = "host_path_default")]
    public string? HostPathDefault { get; set; }

    [YamlMember(Alias = "container_path")]
    public string ContainerPath { get; set; } = "";

    [YamlMember(Alias = "read_only")]
    public bool ReadOnly { get; set; }

    [YamlMember(Alias = "description")]
    public string Description { get; set; } = "";

    [YamlMember(Alias = "is_file")]
    public bool IsFile { get; set; }

    [YamlMember(Alias = "host_path_override")]
    public string? HostPathOverride { get; set; }
}

/// <summary>
/// Environment variable configuration
/// </summary>
public class EnvironmentVariable
{
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = "";

    [YamlMember(Alias = "value")]
    public string Value { get; set; } = "";

    [YamlMember(Alias = "description")]
    public string? Description { get; set; }

    [YamlMember(Alias = "sensitive")]
    public bool Sensitive { get; set; }

    [YamlMember(Alias = "value_override")]
    public string? ValueOverride { get; set; }
}

/// <summary>
/// Service parameter (CLI flag)
/// </summary>
public class ServiceParameter
{
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = "";

    [YamlMember(Alias = "cli_flag")]
    public string? CliFlag { get; set; }

    [YamlMember(Alias = "cli_alias")]
    public string? CliAlias { get; set; }

    [YamlMember(Alias = "type")]
    public string Type { get; set; } = "string";

    [YamlMember(Alias = "default")]
    public object? Default { get; set; }

    [YamlMember(Alias = "description")]
    public string Description { get; set; } = "";

    [YamlMember(Alias = "category")]
    public string? Category { get; set; }

    [YamlMember(Alias = "required")]
    public bool Required { get; set; }

    [YamlMember(Alias = "sensitive")]
    public bool Sensitive { get; set; }

    [YamlMember(Alias = "enum")]
    public List<string>? Enum { get; set; }

    [YamlMember(Alias = "validation")]
    public ValidationConfig? Validation { get; set; }

    [YamlMember(Alias = "feature")]
    public string? Feature { get; set; }

    [YamlMember(Alias = "hidden")]
    public bool Hidden { get; set; }

    [YamlMember(Alias = "use_equals")]
    public bool? UseEquals { get; set; }

    [YamlMember(Alias = "value")]
    public object? Value { get; set; }
    
    /// <summary>
    /// Check if this parameter has been set by the user
    /// </summary>
    public bool HasUserValue => Value != null;
}

/// <summary>
/// Validation configuration for parameters
/// </summary>
public class ValidationConfig
{
    [YamlMember(Alias = "min")]
    public int? Min { get; set; }

    [YamlMember(Alias = "max")]
    public int? Max { get; set; }
}

/// <summary>
/// Configuration file definition
/// </summary>
public class ConfigFileDefinition
{
    [YamlMember(Alias = "format")]
    public string Format { get; set; } = "yaml";

    [YamlMember(Alias = "path_setting")]
    public string? PathSetting { get; set; }

    [YamlMember(Alias = "options")]
    public List<ConfigOption>? Options { get; set; }
}

/// <summary>
/// Configuration file option
/// </summary>
public class ConfigOption
{
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = "";

    [YamlMember(Alias = "type")]
    public string Type { get; set; } = "string";

    [YamlMember(Alias = "default")]
    public object? Default { get; set; }

    [YamlMember(Alias = "description")]
    public string Description { get; set; } = "";

    [YamlMember(Alias = "category")]
    public string? Category { get; set; }

    [YamlMember(Alias = "optional")]
    public bool Optional { get; set; }

    [YamlMember(Alias = "properties")]
    public List<ConfigOption>? Properties { get; set; }

    [YamlMember(Alias = "items")]
    public ConfigOptionItems? Items { get; set; }
}

/// <summary>
/// Config option items for array types
/// </summary>
public class ConfigOptionItems
{
    [YamlMember(Alias = "type")]
    public string Type { get; set; } = "string";

    [YamlMember(Alias = "properties")]
    public List<ConfigOption>? Properties { get; set; }
}

/// <summary>
/// Service dependency configuration
/// </summary>
public class ServiceDependency
{
    [YamlMember(Alias = "service_id")]
    public string ServiceId { get; set; } = "";

    [YamlMember(Alias = "condition")]
    public string Condition { get; set; } = "running";

    [YamlMember(Alias = "reason")]
    public string? Reason { get; set; }
}

/// <summary>
/// Health check configuration
/// </summary>
public class HealthCheckConfig
{
    [YamlMember(Alias = "command")]
    public string Command { get; set; } = "";

    [YamlMember(Alias = "interval_seconds")]
    public int IntervalSeconds { get; set; } = 30;

    [YamlMember(Alias = "timeout_seconds")]
    public int TimeoutSeconds { get; set; } = 10;

    [YamlMember(Alias = "retries")]
    public int Retries { get; set; } = 3;

    [YamlMember(Alias = "start_period_seconds")]
    public int StartPeriodSeconds { get; set; } = 60;
}

/// <summary>
/// Menu configuration for a service
/// </summary>
public class MenuConfig
{
    [YamlMember(Alias = "main_menu_order")]
    public int MainMenuOrder { get; set; }

    [YamlMember(Alias = "submenu")]
    public List<SubmenuItem>? Submenu { get; set; }
}

/// <summary>
/// Submenu item configuration
/// </summary>
public class SubmenuItem
{
    [YamlMember(Alias = "action")]
    public string Action { get; set; } = "";

    [YamlMember(Alias = "id")]
    public string? Id { get; set; }

    [YamlMember(Alias = "label")]
    public string? Label { get; set; }

    [YamlMember(Alias = "label_running")]
    public string? LabelRunning { get; set; }

    [YamlMember(Alias = "label_stopped")]
    public string? LabelStopped { get; set; }

    [YamlMember(Alias = "handler")]
    public string? Handler { get; set; }

    [YamlMember(Alias = "command")]
    public CustomCommand? Command { get; set; }
}

/// <summary>
/// Custom command definition
/// </summary>
public class CustomCommand
{
    [YamlMember(Alias = "binary")]
    public string Binary { get; set; } = "";

    [YamlMember(Alias = "arguments")]
    public List<string>? Arguments { get; set; }

    [YamlMember(Alias = "inputs")]
    public List<CommandInput>? Inputs { get; set; }

    [YamlMember(Alias = "description")]
    public string? Description { get; set; }

    [YamlMember(Alias = "show_output")]
    public bool ShowOutput { get; set; } = true;
}

/// <summary>
/// User input definition for custom commands
/// </summary>
public class CommandInput
{
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = "";

    [YamlMember(Alias = "prompt")]
    public string Prompt { get; set; } = "";

    [YamlMember(Alias = "type")]
    public string Type { get; set; } = "string";

    [YamlMember(Alias = "default")]
    public string? Default { get; set; }

    [YamlMember(Alias = "required")]
    public bool Required { get; set; } = true;

    [YamlMember(Alias = "description")]
    public string? Description { get; set; }

    [YamlMember(Alias = "pattern")]
    public string? Pattern { get; set; }
}

/// <summary>
/// Service setting configuration
/// </summary>
public class ServiceSetting
{
    [YamlMember(Alias = "key")]
    public string Key { get; set; } = "";

    [YamlMember(Alias = "setting_path")]
    public string SettingPath { get; set; } = "";

    [YamlMember(Alias = "type")]
    public string Type { get; set; } = "string";

    [YamlMember(Alias = "category")]
    public string? Category { get; set; }

    [YamlMember(Alias = "description")]
    public string? Description { get; set; }

    [YamlMember(Alias = "validation")]
    public ValidationConfig? Validation { get; set; }
}

/// <summary>
/// Category definition
/// </summary>
public class CategoryDefinition
{
    [YamlMember(Alias = "id")]
    public string Id { get; set; } = "";

    [YamlMember(Alias = "name")]
    public string Name { get; set; } = "";

    [YamlMember(Alias = "description")]
    public string Description { get; set; } = "";

    [YamlMember(Alias = "order")]
    public int Order { get; set; }
}

/// <summary>
/// Parameter category definition
/// </summary>
public class ParameterCategoryDefinition
{
    [YamlMember(Alias = "id")]
    public string Id { get; set; } = "";

    [YamlMember(Alias = "name")]
    public string Name { get; set; } = "";

    [YamlMember(Alias = "description")]
    public string Description { get; set; } = "";
}

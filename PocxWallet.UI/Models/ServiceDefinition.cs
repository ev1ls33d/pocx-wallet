namespace PocxWallet.UI.Models;

/// <summary>
/// Service definition for UI display
/// </summary>
public class ServiceDefinition
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<ServiceParameter>? Parameters { get; set; }
}

/// <summary>
/// Service parameter
/// </summary>
public class ServiceParameter
{
    public string? Name { get; set; }
    public object? Value { get; set; }
    public object? Default { get; set; }
}

/// <summary>
/// Service configuration
/// </summary>
public class ServiceConfiguration
{
    public List<ServiceDefinition>? Services { get; set; }
}

namespace PocxWallet.Core.Services;

/// <summary>
/// Abstraction for service logging, allowing different implementations for CLI and UI
/// </summary>
public interface IServiceLogger
{
    /// <summary>
    /// Log an informational message
    /// </summary>
    void LogInfo(string message);
    
    /// <summary>
    /// Log a success message
    /// </summary>
    void LogSuccess(string message);
    
    /// <summary>
    /// Log a warning message
    /// </summary>
    void LogWarning(string message);
    
    /// <summary>
    /// Log an error message
    /// </summary>
    void LogError(string message);
    
    /// <summary>
    /// Log a debug/dim message
    /// </summary>
    void LogDebug(string message);
}

/// <summary>
/// Silent logger implementation that does nothing (for UI or testing)
/// </summary>
public class SilentServiceLogger : IServiceLogger
{
    public static readonly SilentServiceLogger Instance = new();
    
    public void LogInfo(string message) { }
    public void LogSuccess(string message) { }
    public void LogWarning(string message) { }
    public void LogError(string message) { }
    public void LogDebug(string message) { }
}

/// <summary>
/// Console logger implementation using standard Console output
/// </summary>
public class ConsoleServiceLogger : IServiceLogger
{
    public static readonly ConsoleServiceLogger Instance = new();
    
    public void LogInfo(string message) => Console.WriteLine(message);
    public void LogSuccess(string message) => Console.WriteLine($"? {message}");
    public void LogWarning(string message) => Console.WriteLine($"? {message}");
    public void LogError(string message) => Console.Error.WriteLine($"? {message}");
    public void LogDebug(string message) => Console.WriteLine($"  {message}");
}

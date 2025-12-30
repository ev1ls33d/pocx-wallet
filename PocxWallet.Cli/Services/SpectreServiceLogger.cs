using PocxWallet.Core.Services;
using Spectre.Console;

namespace PocxWallet.Cli.Services;

/// <summary>
/// Service logger implementation using Spectre.Console for CLI output
/// </summary>
public class SpectreServiceLogger : IServiceLogger
{
    public static readonly SpectreServiceLogger Instance = new();
    
    public void LogInfo(string message)
    {
        AnsiConsole.MarkupLine($"[bold]{Markup.Escape(message)}[/]");
    }

    public void LogSuccess(string message)
    {
        AnsiConsole.MarkupLine($"[green]?[/] {Markup.Escape(message)}");
    }

    public void LogWarning(string message)
    {
        AnsiConsole.MarkupLine($"[yellow]{Markup.Escape(message)}[/]");
    }

    public void LogError(string message)
    {
        AnsiConsole.MarkupLine($"[red]{Markup.Escape(message)}[/]");
    }

    public void LogDebug(string message)
    {
        AnsiConsole.MarkupLine($"[dim]{Markup.Escape(message)}[/]");
    }
}

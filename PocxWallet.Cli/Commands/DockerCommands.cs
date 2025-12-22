using PocxWallet.Cli.Services;
using PocxWallet.Cli.Configuration;
using Spectre.Console;

namespace PocxWallet.Cli.Commands;

/// <summary>
/// Docker container management commands
/// </summary>
public static class DockerCommands
{
    private static DockerServiceManager? _dockerManager;

    private static DockerServiceManager GetDockerManager()
    {
        if (_dockerManager == null)
        {
            _dockerManager = new DockerServiceManager();
        }
        return _dockerManager;
    }

    /// <summary>
    /// Check Docker installation and status
    /// </summary>
    public static async Task CheckDockerStatusAsync(AppSettings settings)
    {
        var docker = GetDockerManager();

        AnsiConsole.MarkupLine("[bold]Checking Docker status...[/]");
        AnsiConsole.WriteLine();

        var available = await docker.IsDockerAvailableAsync();
        if (available)
        {
            AnsiConsole.MarkupLine("[green]✓[/] Docker is installed and running");
            
            // List PoCX containers
            var containers = await docker.ListContainersAsync();
            if (containers.Any())
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[bold]PoCX Containers:[/]");
                
                var table = new Table();
                table.Border(TableBorder.Rounded);
                table.AddColumn("Name");
                table.AddColumn("Status");
                table.AddColumn("Ports");

                foreach (var container in containers)
                {
                    var statusColor = container.Status.Contains("Up") ? "green" : "red";
                    table.AddRow(
                        container.Name,
                        $"[{statusColor}]{container.Status}[/]",
                        container.Ports
                    );
                }

                AnsiConsole.Write(table);
            }
            else
            {
                AnsiConsole.MarkupLine("[dim]No PoCX containers found[/]");
            }
        }
        else
        {
            AnsiConsole.MarkupLine("[red]✗[/] Docker is not available");
        }
    }

    /// <summary>
    /// Setup Docker (install if needed)
    /// </summary>
    public static async Task SetupDockerAsync(AppSettings settings)
    {
        var docker = GetDockerManager();
        
        await docker.EnsureDockerInstalledAsync();
    }

}

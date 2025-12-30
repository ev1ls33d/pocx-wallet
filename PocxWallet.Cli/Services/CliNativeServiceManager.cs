using PocxWallet.Core.Services;
using PocxWallet.Cli.Configuration;
using PocxWallet.Cli.Resources;
using Spectre.Console;

namespace PocxWallet.Cli.Services;

/// <summary>
/// CLI-specific native service manager with Spectre.Console output
/// </summary>
public class CliNativeServiceManager : NativeServiceManager
{
    public CliNativeServiceManager() : base(SpectreServiceLogger.Instance)
    {
    }

    /// <summary>
    /// Download and extract with progress display
    /// </summary>
    public async Task<bool> DownloadAndExtractNativeAsync(
        string url,
        string version,
        string serviceId,
        string serviceName,
        List<string>? whitelist = null)
    {
        var fileName = Path.GetFileName(new Uri(url).LocalPath);
        
        return await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask($"Downloading {fileName}", maxValue: 100);
                
                var result = await DownloadAndExtractNativeAsync(
                    url, version, serviceId, serviceName, whitelist,
                    progress => task.Value = progress);
                
                task.Value = 100;
                return result;
            });
    }
}

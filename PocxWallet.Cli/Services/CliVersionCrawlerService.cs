using PocxWallet.Core.Services;
using PocxWallet.Cli.Configuration;
using PocxWallet.Cli.Resources;
using Spectre.Console;

namespace PocxWallet.Cli.Services;

/// <summary>
/// CLI-specific version crawler with Spectre.Console prompts
/// </summary>
public class CliVersionCrawlerService : VersionCrawlerService
{
    private readonly WalletManager _walletManager;

    public CliVersionCrawlerService() : base(SpectreServiceLogger.Instance)
    {
        _walletManager = WalletManager.Instance;
        
        // Load token from wallet settings
        var storedToken = _walletManager.Settings.GitHubToken;
        if (!string.IsNullOrWhiteSpace(storedToken))
        {
            SetGitHubToken(storedToken);
        }
        
        // Set up token saving
        SaveTokenAction = token =>
        {
            _walletManager.Settings.GitHubToken = token;
            _walletManager.Save();
        };
        
        // Set up authentication prompt using += for event subscription
        base.OnAuthenticationRequired += PromptForGitHubTokenAsync;
    }

    private Task<string?> PromptForGitHubTokenAsync()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(Strings.VersionCrawler.TokenInfo);
        AnsiConsole.MarkupLine(Strings.VersionCrawler.TokenRequired);
        AnsiConsole.WriteLine();
        
        var token = AnsiConsole.Prompt(
            new TextPrompt<string>(Strings.VersionCrawler.EnterGitHubToken).AllowEmpty());
        
        if (!string.IsNullOrWhiteSpace(token))
        {
            AnsiConsole.MarkupLine(Strings.VersionCrawler.AuthenticationSuccess);
            AnsiConsole.WriteLine();
        }
        
        return Task.FromResult<string?>(string.IsNullOrWhiteSpace(token) ? null : token);
    }
}

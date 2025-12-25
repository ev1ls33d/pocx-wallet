using PocxWallet.Cli.Configuration;
using PocxWallet.Cli.Resources;
using Spectre.Console;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace PocxWallet.Cli.Services;

/// <summary>
/// Service for dynamically discovering service versions from GitHub repositories and container registries
/// </summary>
public class VersionCrawlerService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, CachedResult> _cache;
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);
    private bool _disposed;
    private string? _githubToken;
    private readonly WalletManager _walletManager;

    public VersionCrawlerService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "PocxWallet/1.0");
        _cache = new Dictionary<string, CachedResult>();
        _walletManager = WalletManager.Instance;
        
        // Load token from wallet settings if available
        var storedToken = _walletManager.Settings.GitHubToken;
        if (!string.IsNullOrWhiteSpace(storedToken))
        {
            SetGitHubToken(storedToken);
        }
    }

    /// <summary>
    /// Set GitHub authentication token for API access and save it to wallet settings
    /// </summary>
    public void SetGitHubToken(string token)
    {
        _githubToken = token;
        
        // Remove existing Authorization header if present
        _httpClient.DefaultRequestHeaders.Remove("Authorization");
        
        // Add new Authorization header with Bearer token
        if (!string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            
            // Save token to wallet settings
            _walletManager.Settings.GitHubToken = token;
            _walletManager.Save();
        }
    }

    /// <summary>
    /// Crawl GitHub Releases page and extract download URLs matching the filter
    /// </summary>
    /// <param name="repositoryUrl">GitHub repository URL (e.g., https://github.com/owner/repo/releases/)</param>
    /// <param name="filterRegex">Regex pattern to match against asset filenames</param>
    /// <param name="releaseTag">Specific release tag to fetch, or null for latest</param>
    /// <param name="whitelist">Optional whitelist of files to extract from archives</param>
    /// <returns>List of native downloads discovered</returns>
    public async Task<List<NativeDownload>> CrawlGitHubReleasesAsync(
        string repositoryUrl,
        string filterRegex,
        string? releaseTag = null,
        List<string>? whitelist = null)
    {
        var cacheKey = $"github-releases:{repositoryUrl}:{filterRegex}:{releaseTag}";
        
        // Check cache
        if (_cache.TryGetValue(cacheKey, out var cachedResult))
        {
            if (DateTime.UtcNow - cachedResult.Timestamp < CacheExpiration)
            {
                return (List<NativeDownload>)cachedResult.Data;
            }
        }

        try
        {
            // Parse repository URL to extract owner and repo
            var (owner, repo) = ParseGitHubRepoUrl(repositoryUrl);
            if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
            {
                AnsiConsole.MarkupLine(string.Format(Strings.VersionCrawler.FailedToParseRepoUrl, Markup.Escape(repositoryUrl)));
                return new List<NativeDownload>();
            }

            // Build GitHub API URL
            var apiUrl = releaseTag != null
                ? $"https://api.github.com/repos/{owner}/{repo}/releases/tags/{releaseTag}"
                : $"https://api.github.com/repos/{owner}/{repo}/releases/latest";

            var response = await _httpClient.GetAsync(apiUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                AnsiConsole.MarkupLine(string.Format(Strings.VersionCrawler.GitHubApiRequestFailed, response.StatusCode));
                return new List<NativeDownload>();
            }

            var releaseData = await response.Content.ReadFromJsonAsync<JsonElement>();
            var downloads = new List<NativeDownload>();
            
            // Extract release tag
            var tag = releaseData.GetProperty("tag_name").GetString() ?? "unknown";
            
            // Get assets
            if (releaseData.TryGetProperty("assets", out var assets))
            {
                var regex = new Regex(filterRegex, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                
                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.GetProperty("name").GetString() ?? "";
                    var downloadUrl = asset.GetProperty("browser_download_url").GetString() ?? "";
                    
                    // Apply filter
                    if (!regex.IsMatch(name))
                    {
                        continue;
                    }
                    
                    // Auto-detect platform from filename
                    var platform = DetectPlatformFromFilename(name);
                    
                    downloads.Add(new NativeDownload
                    {
                        Url = downloadUrl,
                        Version = tag,
                        Platform = platform,
                        Description = $"Release {tag} - {name}",
                        Whitelist = whitelist
                    });
                }
            }

            // Cache results
            _cache[cacheKey] = new CachedResult { Data = downloads, Timestamp = DateTime.UtcNow };
            
            return downloads;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine(string.Format(Strings.VersionCrawler.ErrorCrawlingReleases, Markup.Escape(ex.Message)));
            return new List<NativeDownload>();
        }
    }

    /// <summary>
    /// Crawl GitHub Container Registry and extract package versions matching the filter
    /// </summary>
    /// <param name="packageUrl">GitHub Container Registry package URL</param>
    /// <param name="filterRegex">Regex pattern to match against version tags</param>
    /// <returns>List of Docker images discovered</returns>
    public async Task<List<DockerImage>> CrawlContainerRegistryAsync(
        string packageUrl,
        string filterRegex)
    {
        var cacheKey = $"ghcr:{packageUrl}:{filterRegex}";
        
        // Check cache
        if (_cache.TryGetValue(cacheKey, out var cachedResult))
        {
            if (DateTime.UtcNow - cachedResult.Timestamp < CacheExpiration)
            {
                return (List<DockerImage>)cachedResult.Data;
            }
        }

        try
        {
            // Parse package URL to extract owner, package name
            var (owner, packageName, repository, imageName) = ParseGhcrPackageUrl(packageUrl);
            if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(packageName))
            {
                AnsiConsole.MarkupLine(string.Format(Strings.VersionCrawler.FailedToParseGhcrUrl, Markup.Escape(packageUrl)));
                return new List<DockerImage>();
            }

            // For GitHub Container Registry, we'll try to use the GitHub Packages API
            // Note: This requires authentication for private packages
            // For public packages, we can try to scrape the package page or use Docker registry API
            
            // Attempt to use GitHub Packages API
            var apiUrl = $"https://api.github.com/users/{Uri.EscapeDataString(owner)}/packages/container/{Uri.EscapeDataString(packageName)}/versions";
            
            var response = await _httpClient.GetAsync(apiUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                // Check if authentication is required
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || 
                    response.StatusCode == System.Net.HttpStatusCode.Forbidden ||
                    response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // Prompt for authentication if we haven't tried yet
                    if (string.IsNullOrEmpty(_githubToken))
                    {
                        AnsiConsole.MarkupLine(string.Format(Strings.VersionCrawler.GitHubPackagesApiAuthRequired, response.StatusCode));
                        AnsiConsole.WriteLine();
                        
                        AnsiConsole.MarkupLine(Strings.VersionCrawler.TokenInfo);
                        AnsiConsole.MarkupLine(Strings.VersionCrawler.TokenRequired);
                        AnsiConsole.WriteLine();
                            
                        var token = AnsiConsole.Prompt(
                            new TextPrompt<string>(Strings.VersionCrawler.EnterGitHubToken).AllowEmpty());
                            
                        if (!string.IsNullOrWhiteSpace(token))
                        {
                            SetGitHubToken(token);
                                
                            // Retry the request with authentication
                            response = await _httpClient.GetAsync(apiUrl);
                                
                            if (response.IsSuccessStatusCode)
                            {
                                AnsiConsole.MarkupLine(Strings.VersionCrawler.AuthenticationSuccess);
                                AnsiConsole.WriteLine();
                                // Continue with successful response processing below
                            }
                            else
                            {
                                AnsiConsole.MarkupLine(Strings.VersionCrawler.AuthenticationFailed);
                                AnsiConsole.MarkupLine(Strings.VersionCrawler.OnlyLatestTagAvailable);
                                AnsiConsole.WriteLine();
                                    
                                // Return only 'latest' tag as fallback
                                var regex = new Regex(filterRegex, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                                if (regex.IsMatch("latest"))
                                {
                                    return new List<DockerImage>
                                    {
                                        new DockerImage
                                        {
                                            Repository = repository,
                                            Image = imageName,
                                            Tag = "latest",
                                            Description = "Latest version"
                                        }
                                    };
                                }
                                return new List<DockerImage>();
                            }
                        }
                        else
                        {
                            // User skipped authentication
                            AnsiConsole.MarkupLine(Strings.VersionCrawler.OnlyLatestTagAvailable);
                            AnsiConsole.WriteLine();
                                
                            // Return only 'latest' tag as fallback
                            var regex = new Regex(filterRegex, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                            if (regex.IsMatch("latest"))
                            {
                                return new List<DockerImage>
                                {
                                    new DockerImage
                                    {
                                        Repository = repository,
                                        Image = imageName,
                                        Tag = "latest",
                                        Description = "Latest version"
                                    }
                                };
                            }
                            return new List<DockerImage>();
                        }
                        
                    }
                    else
                    {
                        // Already have a token but still failed
                        AnsiConsole.MarkupLine(Strings.VersionCrawler.AuthenticationFailed);
                        AnsiConsole.MarkupLine(Strings.VersionCrawler.OnlyLatestTagAvailable);
                        
                        // Return only 'latest' tag as fallback
                        var regex = new Regex(filterRegex, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                        if (regex.IsMatch("latest"))
                        {
                            return new List<DockerImage>
                            {
                                new DockerImage
                                {
                                    Repository = repository,
                                    Image = imageName,
                                    Tag = "latest",
                                    Description = "Latest version"
                                }
                            };
                        }
                        return new List<DockerImage>();
                    }
                }
                else
                {
                    // Other error
                    AnsiConsole.MarkupLine(string.Format(Strings.VersionCrawler.GitHubPackagesApiAuthRequired, response.StatusCode));
                    return new List<DockerImage>();
                }
            }

            var versionsData = await response.Content.ReadFromJsonAsync<JsonElement>();
            var images = new List<DockerImage>();
            var regex2 = new Regex(filterRegex, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            
            if (versionsData.ValueKind == JsonValueKind.Array)
            {
                foreach (var version in versionsData.EnumerateArray())
                {
                    if (version.TryGetProperty("metadata", out var metadata) &&
                        metadata.TryGetProperty("container", out var container) &&
                        container.TryGetProperty("tags", out var tags))
                    {
                        foreach (var tag in tags.EnumerateArray())
                        {
                            var tagName = tag.GetString() ?? "";
                            
                            // Apply filter
                            if (!regex2.IsMatch(tagName))
                            {
                                continue;
                            }
                            
                            images.Add(new DockerImage
                            {
                                Repository = repository,
                                Image = imageName,
                                Tag = tagName,
                                Description = $"GHCR - {tagName}"
                            });
                        }
                    }
                }
            }

            // Cache results
            _cache[cacheKey] = new CachedResult { Data = images, Timestamp = DateTime.UtcNow };
            
            return images;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine(string.Format(Strings.VersionCrawler.ErrorCrawlingRegistry, Markup.Escape(ex.Message)));
            
            // Return only 'latest' tag as fallback
            var (_, _, repository, imageName) = ParseGhcrPackageUrl(packageUrl);
            var regex = new Regex(filterRegex, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            if (regex.IsMatch("latest"))
            {
                return new List<DockerImage>
                {
                    new DockerImage
                    {
                        Repository = repository,
                        Image = imageName,
                        Tag = "latest",
                        Description = "Latest version"
                    }
                };
            }
            return new List<DockerImage>();
        }
    }

    /// <summary>
    /// Parse GitHub repository URL to extract owner and repo name
    /// </summary>
    private (string owner, string repo) ParseGitHubRepoUrl(string url)
    {
        try
        {
            // Handle various URL formats:
            // https://github.com/owner/repo/releases/
            // https://github.com/owner/repo/
            // https://github.com/owner/repo
            
            var uri = new Uri(url);
            var segments = uri.AbsolutePath.Trim('/').Split('/');
            
            if (segments.Length >= 2)
            {
                return (segments[0], segments[1]);
            }
        }
        catch (UriFormatException ex)
        {
            AnsiConsole.MarkupLine(string.Format(Strings.VersionCrawler.InvalidUrlFormat, Markup.Escape(ex.Message)));
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine(string.Format(Strings.VersionCrawler.ErrorParsingUrl, Markup.Escape(ex.Message)));
        }
        
        return ("", "");
    }

    /// <summary>
    /// Parse GHCR package URL to extract owner, package name, repository, and image name
    /// </summary>
    private (string owner, string packageName, string repository, string imageName) ParseGhcrPackageUrl(string url)
    {
        try
        {
            // Handle URL format:
            // https://github.com/owner/repo/pkgs/container/repo%2Fimage
            // The package name may be URL-encoded
            
            var uri = new Uri(url);
            var segments = uri.AbsolutePath.Trim('/').Split('/');
            
            if (segments.Length >= 5 && segments[2] == "pkgs" && segments[3] == "container")
            {
                var owner = segments[0];
                var packageName = Uri.UnescapeDataString(segments[4]);
                
                // Extract repository and image from package name
                // Format: "repo/image" or just "image"
                var parts = packageName.Split('/');
                var repository = parts.Length > 1 
                    ? $"ghcr.io/{owner}/{parts[0]}"
                    : $"ghcr.io/{owner}";
                var imageName = parts.Length > 1 ? parts[1] : parts[0];
                
                return (owner, packageName, repository, imageName);
            }
        }
        catch (UriFormatException ex)
        {
            AnsiConsole.MarkupLine(string.Format(Strings.VersionCrawler.InvalidGhcrUrlFormat, Markup.Escape(ex.Message)));
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine(string.Format(Strings.VersionCrawler.ErrorParsingGhcrUrl, Markup.Escape(ex.Message)));
        }
        
        return ("", "", "", "");
    }

    /// <summary>
    /// Detect platform from filename patterns
    /// </summary>
    private string DetectPlatformFromFilename(string filename)
    {
        var lower = filename.ToLower();
        
        // Windows patterns
        if (lower.Contains("windows") || lower.Contains("win") || lower.Contains("msvc"))
        {
            if (lower.Contains("x86_64") || lower.Contains("x64") || lower.Contains("amd64"))
                return "win-x64";
            if (lower.Contains("arm64") || lower.Contains("aarch64"))
                return "win-arm64";
            return "win-x64"; // Default to x64 for Windows
        }
        
        // macOS patterns
        if (lower.Contains("darwin") || lower.Contains("macos") || lower.Contains("osx"))
        {
            if (lower.Contains("arm64") || lower.Contains("aarch64"))
                return "osx-arm64";
            if (lower.Contains("x86_64") || lower.Contains("x64") || lower.Contains("amd64"))
                return "osx-x64";
            return "osx-x64"; // Default to x64 for macOS
        }
        
        // Linux patterns (check last to avoid false positives)
        if (lower.Contains("linux") || lower.Contains("gnu") || lower.Contains("musl"))
        {
            if (lower.Contains("x86_64") || lower.Contains("x64") || lower.Contains("amd64"))
                return "linux-x64";
            if (lower.Contains("arm64") || lower.Contains("aarch64"))
                return "linux-arm64";
            if (lower.Contains("armv7") || lower.Contains("armhf"))
                return "linux-arm";
            return "linux-x64"; // Default to x64 for Linux
        }
        
        // Fallback: detect current platform
        return GetCurrentPlatform();
    }

    /// <summary>
    /// Get current platform identifier
    /// </summary>
    private string GetCurrentPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "win-arm64" : "win-x64";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "osx-arm64" : "osx-x64";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.Arm64 => "linux-arm64",
                Architecture.Arm => "linux-arm",
                _ => "linux-x64"
            };
        }
        
        return "unknown";
    }

    /// <summary>
    /// Cache result with timestamp
    /// </summary>
    private class CachedResult
    {
        public object Data { get; set; } = null!;
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Dispose of resources
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Dispose pattern implementation
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _httpClient?.Dispose();
            }
            _disposed = true;
        }
    }
}

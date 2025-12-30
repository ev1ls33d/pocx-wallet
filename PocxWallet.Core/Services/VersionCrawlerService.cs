using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PocxWallet.Core.Services;

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
    private readonly IServiceLogger _logger;
    
    /// <summary>
    /// Event raised when GitHub authentication is required
    /// </summary>
    public event Func<Task<string?>>? OnAuthenticationRequired;
    
    /// <summary>
    /// Action to save the GitHub token (set by caller)
    /// </summary>
    public Action<string>? SaveTokenAction { get; set; }

    public VersionCrawlerService() : this(SilentServiceLogger.Instance)
    {
    }

    public VersionCrawlerService(IServiceLogger logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "PocxWallet/1.0");
        _cache = new Dictionary<string, CachedResult>();
    }

    /// <summary>
    /// Set GitHub authentication token for API access
    /// </summary>
    public void SetGitHubToken(string token)
    {
        _githubToken = token;
        
        _httpClient.DefaultRequestHeaders.Remove("Authorization");
        
        if (!string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            SaveTokenAction?.Invoke(token);
        }
    }

    /// <summary>
    /// Crawl GitHub Releases page and extract download URLs matching the filter
    /// </summary>
    public async Task<List<NativeDownload>> CrawlGitHubReleasesAsync(
        string repositoryUrl,
        string filterRegex,
        string? releaseTag = null,
        List<string>? whitelist = null)
    {
        var cacheKey = $"github-releases:{repositoryUrl}:{filterRegex}:{releaseTag}";
        
        if (_cache.TryGetValue(cacheKey, out var cachedResult))
        {
            if (DateTime.UtcNow - cachedResult.Timestamp < CacheExpiration)
            {
                return (List<NativeDownload>)cachedResult.Data;
            }
        }

        try
        {
            var (owner, repo) = ParseGitHubRepoUrl(repositoryUrl);
            if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
            {
                _logger.LogError($"Failed to parse repository URL: {repositoryUrl}");
                return new List<NativeDownload>();
            }

            var apiUrl = releaseTag != null
                ? $"https://api.github.com/repos/{owner}/{repo}/releases/tags/{releaseTag}"
                : $"https://api.github.com/repos/{owner}/{repo}/releases/latest";

            var response = await _httpClient.GetAsync(apiUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"GitHub API request failed: {response.StatusCode}");
                return new List<NativeDownload>();
            }

            var releaseData = await response.Content.ReadFromJsonAsync<JsonElement>();
            var downloads = new List<NativeDownload>();
            
            var tag = releaseData.GetProperty("tag_name").GetString() ?? "unknown";
            
            if (releaseData.TryGetProperty("assets", out var assets))
            {
                var regex = new Regex(filterRegex, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                
                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.GetProperty("name").GetString() ?? "";
                    var downloadUrl = asset.GetProperty("browser_download_url").GetString() ?? "";
                    
                    if (!regex.IsMatch(name))
                    {
                        continue;
                    }
                    
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

            _cache[cacheKey] = new CachedResult { Data = downloads, Timestamp = DateTime.UtcNow };
            
            return downloads;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error crawling releases: {ex.Message}");
            return new List<NativeDownload>();
        }
    }

    /// <summary>
    /// Crawl GitHub Container Registry and extract package versions matching the filter
    /// </summary>
    public async Task<List<DockerImage>> CrawlContainerRegistryAsync(
        string packageUrl,
        string filterRegex)
    {
        var cacheKey = $"ghcr:{packageUrl}:{filterRegex}";
        
        if (_cache.TryGetValue(cacheKey, out var cachedResult))
        {
            if (DateTime.UtcNow - cachedResult.Timestamp < CacheExpiration)
            {
                return (List<DockerImage>)cachedResult.Data;
            }
        }

        try
        {
            var (owner, packageName, repository, imageName) = ParseGhcrPackageUrl(packageUrl);
            if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(packageName))
            {
                _logger.LogError($"Failed to parse GHCR URL: {packageUrl}");
                return new List<DockerImage>();
            }

            var apiUrl = $"https://api.github.com/users/{Uri.EscapeDataString(owner)}/packages/container/{Uri.EscapeDataString(packageName)}/versions";
            
            var response = await _httpClient.GetAsync(apiUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || 
                    response.StatusCode == System.Net.HttpStatusCode.Forbidden ||
                    response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    if (string.IsNullOrEmpty(_githubToken) && OnAuthenticationRequired != null)
                    {
                        _logger.LogWarning($"GitHub Packages API requires authentication ({response.StatusCode})");
                        
                        var token = await OnAuthenticationRequired.Invoke();
                            
                        if (!string.IsNullOrWhiteSpace(token))
                        {
                            SetGitHubToken(token);
                            response = await _httpClient.GetAsync(apiUrl);
                                
                            if (response.IsSuccessStatusCode)
                            {
                                _logger.LogSuccess("Authentication successful");
                            }
                            else
                            {
                                _logger.LogWarning("Authentication failed, only 'latest' tag available");
                                return CreateLatestTagFallback(repository, imageName, filterRegex);
                            }
                        }
                        else
                        {
                            _logger.LogInfo("Only 'latest' tag available without authentication");
                            return CreateLatestTagFallback(repository, imageName, filterRegex);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Authentication failed, only 'latest' tag available");
                        return CreateLatestTagFallback(repository, imageName, filterRegex);
                    }
                }
                else
                {
                    _logger.LogError($"GitHub Packages API request failed: {response.StatusCode}");
                    return new List<DockerImage>();
                }
            }

            var versionsData = await response.Content.ReadFromJsonAsync<JsonElement>();
            var images = new List<DockerImage>();
            var regex = new Regex(filterRegex, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            
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
                            
                            if (!regex.IsMatch(tagName))
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

            _cache[cacheKey] = new CachedResult { Data = images, Timestamp = DateTime.UtcNow };
            
            return images;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error crawling registry: {ex.Message}");
            
            var (_, _, repository, imageName) = ParseGhcrPackageUrl(packageUrl);
            return CreateLatestTagFallback(repository, imageName, filterRegex);
        }
    }

    private List<DockerImage> CreateLatestTagFallback(string repository, string imageName, string filterRegex)
    {
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

    private (string owner, string repo) ParseGitHubRepoUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var segments = uri.AbsolutePath.Trim('/').Split('/');
            
            if (segments.Length >= 2)
            {
                return (segments[0], segments[1]);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error parsing URL: {ex.Message}");
        }
        
        return ("", "");
    }

    private (string owner, string packageName, string repository, string imageName) ParseGhcrPackageUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var segments = uri.AbsolutePath.Trim('/').Split('/');
            
            if (segments.Length >= 5 && segments[2] == "pkgs" && segments[3] == "container")
            {
                var owner = segments[0];
                var packageName = Uri.UnescapeDataString(segments[4]);
                
                var parts = packageName.Split('/');
                var repository = parts.Length > 1 
                    ? $"ghcr.io/{owner}/{parts[0]}"
                    : $"ghcr.io/{owner}";
                var imageName = parts.Length > 1 ? parts[1] : parts[0];
                
                return (owner, packageName, repository, imageName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error parsing GHCR URL: {ex.Message}");
        }
        
        return ("", "", "", "");
    }

    private string DetectPlatformFromFilename(string filename)
    {
        var lower = filename.ToLower();
        
        if (lower.Contains("windows") || lower.Contains("win") || lower.Contains("msvc"))
        {
            if (lower.Contains("x86_64") || lower.Contains("x64") || lower.Contains("amd64"))
                return "win-x64";
            if (lower.Contains("arm64") || lower.Contains("aarch64"))
                return "win-arm64";
            return "win-x64";
        }
        
        if (lower.Contains("darwin") || lower.Contains("macos") || lower.Contains("osx"))
        {
            if (lower.Contains("arm64") || lower.Contains("aarch64"))
                return "osx-arm64";
            if (lower.Contains("x86_64") || lower.Contains("x64") || lower.Contains("amd64"))
                return "osx-x64";
            return "osx-x64";
        }
        
        if (lower.Contains("linux") || lower.Contains("gnu") || lower.Contains("musl"))
        {
            if (lower.Contains("x86_64") || lower.Contains("x64") || lower.Contains("amd64"))
                return "linux-x64";
            if (lower.Contains("arm64") || lower.Contains("aarch64"))
                return "linux-arm64";
            if (lower.Contains("armv7") || lower.Contains("armhf"))
                return "linux-arm";
            return "linux-x64";
        }
        
        return NativeServiceManager.GetCurrentPlatform();
    }

    private class CachedResult
    {
        public object Data { get; set; } = null!;
        public DateTime Timestamp { get; set; }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

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

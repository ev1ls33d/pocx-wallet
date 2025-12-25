using PocxWallet.Cli.Configuration;
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
    private static readonly string[] FallbackTags = new[] { "latest", "0.7.0", "0.6.0", "0.5.0" };
    private bool _disposed;

    public VersionCrawlerService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "PocxWallet/1.0");
        _cache = new Dictionary<string, CachedResult>();
    }

    /// <summary>
    /// Crawl GitHub Releases page and extract download URLs matching the filter
    /// </summary>
    /// <param name="repositoryUrl">GitHub repository URL (e.g., https://github.com/owner/repo/releases/)</param>
    /// <param name="filterRegex">Regex pattern to match against asset filenames</param>
    /// <param name="releaseTag">Specific release tag to fetch, or null for latest</param>
    /// <returns>List of native downloads discovered</returns>
    public async Task<List<NativeDownload>> CrawlGitHubReleasesAsync(
        string repositoryUrl,
        string filterRegex,
        string? releaseTag = null)
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
                Console.WriteLine($"[yellow]Failed to parse GitHub repository URL: {repositoryUrl}[/]");
                return new List<NativeDownload>();
            }

            // Build GitHub API URL
            var apiUrl = releaseTag != null
                ? $"https://api.github.com/repos/{owner}/{repo}/releases/tags/{releaseTag}"
                : $"https://api.github.com/repos/{owner}/{repo}/releases/latest";

            var response = await _httpClient.GetAsync(apiUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[yellow]GitHub API request failed: {response.StatusCode}[/]");
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
                        Description = $"{tag} - {platform}"
                    });
                }
            }

            // Cache results
            _cache[cacheKey] = new CachedResult { Data = downloads, Timestamp = DateTime.UtcNow };
            
            return downloads;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[red]Error crawling GitHub releases: {ex.Message}[/]");
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
                Console.WriteLine($"[yellow]Failed to parse GHCR package URL: {packageUrl}[/]");
                return new List<DockerImage>();
            }

            // For GitHub Container Registry, we'll try to use the GitHub Packages API
            // Note: This requires authentication for private packages
            // For public packages, we can try to scrape the package page or use Docker registry API
            
            // Attempt to use GitHub Packages API
            var apiUrl = $"https://api.github.com/users/{owner}/packages/container/{packageName}/versions";
            
            var response = await _httpClient.GetAsync(apiUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[yellow]GitHub Packages API request failed: {response.StatusCode}[/]");
                Console.WriteLine($"[dim]Attempting fallback to common tags...[/]");
                return GetFallbackDockerTags(repository, imageName, filterRegex);
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
                            
                            // Apply filter
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

            // Cache results
            _cache[cacheKey] = new CachedResult { Data = images, Timestamp = DateTime.UtcNow };
            
            return images;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[red]Error crawling container registry: {ex.Message}[/]");
            Console.WriteLine($"[dim]Attempting fallback to common tags...[/]");
            
            // Fallback to common tags
            var (_, _, repository, imageName) = ParseGhcrPackageUrl(packageUrl);
            return GetFallbackDockerTags(repository, imageName, filterRegex);
        }
    }

    /// <summary>
    /// Fallback method to provide common Docker tags when API fails
    /// </summary>
    private List<DockerImage> GetFallbackDockerTags(string repository, string imageName, string filterRegex)
    {
        var regex = new Regex(filterRegex, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        var images = new List<DockerImage>();
        
        foreach (var tag in FallbackTags)
        {
            if (regex.IsMatch(tag))
            {
                images.Add(new DockerImage
                {
                    Repository = repository,
                    Image = imageName,
                    Tag = tag,
                    Description = $"Common tag - {tag}"
                });
            }
        }
        
        return images;
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
            Console.WriteLine($"[dim]Invalid URL format: {ex.Message}[/]");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[dim]Error parsing URL: {ex.Message}[/]");
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
            Console.WriteLine($"[dim]Invalid GHCR URL format: {ex.Message}[/]");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[dim]Error parsing GHCR URL: {ex.Message}[/]");
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

using IStillDontCareAboutCookies.Api.Services.Interfaces;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace IStillDontCareAboutCookies.Api.Services;

public class NSFWChecker : INSFWChecker
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NSFWChecker> _logger;
    private readonly NSFWCheckerOptions _options;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);

    private ConcurrentDictionary<string, bool> _nsfwHostnames = new();
    private bool _isInitialized;
    private DateTime _lastRefreshTime = DateTime.MinValue;

    public NSFWChecker(
        HttpClient httpClient,
        ILogger<NSFWChecker> logger,
        IOptions<NSFWCheckerOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;

        _httpClient.Timeout = TimeSpan.FromSeconds(_options.HttpTimeoutSeconds);
    }

    public async Task<HashSet<string>> FetchList()
    {
        try
        {
            _logger.LogInformation("Fetching NSFW hostname list from {Url}", _options.ListUrl);

            string response = await _httpClient.GetStringAsync(_options.ListUrl);

            var hostnames = response
                .Split('\n')
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith('#'))
                .Select(hostname => hostname.ToLowerInvariant())
                .ToHashSet();

            _logger.LogInformation("Successfully fetched {Count} NSFW hostnames", hostnames.Count);

            return hostnames;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch NSFW hostname list from {Url}", _options.ListUrl);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout while fetching NSFW hostname list from {Url}", _options.ListUrl);
            throw;
        }
    }

    public bool IsHostnameNSFW(string hostname)
    {
        if (string.IsNullOrWhiteSpace(hostname))
        {
            return false;
        }

        if (!_isInitialized)
        {
            _logger.LogWarning("NSFW checker not initialized yet, returning false for hostname: {Hostname}", hostname);
            return false;
        }

        var normalizedHostname = NormalizeHostname(hostname);

        return _nsfwHostnames.ContainsKey(normalizedHostname);
    }

    public async Task EnsureInitializedAsync()
    {
        if (_isInitialized && !ShouldRefresh())
        {
            return;
        }

        await _initializationLock.WaitAsync();
        try
        {
            if (_isInitialized && !ShouldRefresh())
            {
                return;
            }

            await RefreshListAsync();
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    private async Task RefreshListAsync()
    {
        try
        {
            var hostnames = await FetchList();

            var newDictionary = new ConcurrentDictionary<string, bool>(
                hostnames.Select(h => new KeyValuePair<string, bool>(h, true))
            );

            _nsfwHostnames = newDictionary;
            _lastRefreshTime = DateTime.UtcNow;
            _isInitialized = true;

            _logger.LogInformation("NSFW hostname list refreshed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh NSFW hostname list");

            if (!_isInitialized)
            {
                _logger.LogWarning("Initializing with empty NSFW list due to fetch failure");
                _nsfwHostnames = new ConcurrentDictionary<string, bool>();
                _isInitialized = true;
            }

            throw;
        }
    }

    private bool ShouldRefresh()
    {
        if (!_isInitialized)
        {
            return true;
        }

        var timeSinceRefresh = DateTime.UtcNow - _lastRefreshTime;
        var refreshInterval = TimeSpan.FromHours(_options.RefreshIntervalHours);

        return timeSinceRefresh >= refreshInterval;
    }

    private static string NormalizeHostname(string hostname)
    {
        var normalized = hostname.Trim().ToLowerInvariant();

        if (normalized.StartsWith("www."))
        {
            normalized = normalized.Substring(4);
        }

        return normalized;
    }
}

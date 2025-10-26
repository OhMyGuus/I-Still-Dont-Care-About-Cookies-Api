using IStillDontCareAboutCookies.Api.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace IStillDontCareAboutCookies.Api.Services;

public class NSFWCheckerBackgroundService : BackgroundService
{
    private readonly ILogger<NSFWCheckerBackgroundService> _logger;
    private readonly NSFWCheckerOptions _options;
    private readonly IServiceProvider _serviceProvider;

    public NSFWCheckerBackgroundService(
        ILogger<NSFWCheckerBackgroundService> logger,
        IOptions<NSFWCheckerOptions> options,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _options = options.Value;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NSFW Checker Background Service starting");

        await LoadNSFWListAsync(stoppingToken);

        var refreshInterval = TimeSpan.FromHours(_options.RefreshIntervalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(refreshInterval, stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                {
                    await LoadNSFWListAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("NSFW Checker Background Service is stopping");
                break;
            }
        }
    }

    private async Task LoadNSFWListAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var nsfwChecker = scope.ServiceProvider.GetRequiredService<INSFWChecker>();

            await nsfwChecker.EnsureInitializedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading NSFW list in background service");
        }
    }
}

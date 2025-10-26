using IStillDontCareAboutCookies.Api.Models.Configuration;
using IStillDontCareAboutCookies.Api.Services.Interfaces;

namespace IStillDontCareAboutCookies.Api.Services;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReportService(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GithubConfiguration>(configuration.GetSection(GithubConfiguration.ConfigurationKey));
        services.Configure<NSFWCheckerOptions>(configuration.GetSection(NSFWCheckerOptions.SectionName));

        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IGithubService, GithubService>();

        services.AddHttpClient<NSFWChecker>();
        services.AddSingleton<INSFWChecker, NSFWChecker>();
        services.AddHostedService<NSFWCheckerBackgroundService>();

        return services;
    }
}

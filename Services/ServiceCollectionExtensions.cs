using IStillDontCareAboutCookies.Api.Models.Configuration;
using IStillDontCareAboutCookies.Api.Services.Interfaces;

namespace IStillDontCareAboutCookies.Api.Services;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReportService(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GithubConfiguration>(configuration.GetSection(GithubConfiguration.ConfigurationKey));
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<INSFWChecker, NSFWChecker>();
        services.AddScoped<IGithubService, GithubService>();
        return services;
    }
}

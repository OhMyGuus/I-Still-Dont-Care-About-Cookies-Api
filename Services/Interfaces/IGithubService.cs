using IStillDontCareAboutCookies.Api.Models;

namespace IStillDontCareAboutCookies.Api.Services.Interfaces;

public interface IGithubService
{
    Task<string?> ReportWebsiteAsync(ReportModel report, string browser);
}
using IStillDontCareAboutCookies.Api.Models;
using IStillDontCareAboutCookies.Api.Services.Interfaces;

namespace IStillDontCareAboutCookies.Api.Services;

public class ReportService : IReportService
{
    private readonly IGithubService _githubService;

    public ReportService(IGithubService githubService)
    {
        _githubService = githubService;
    }

    public async Task<ReportResponse> Report(ReportModel report, string browser)
    {
        if (report.GetUri() == null)
            return new ReportResponse() { Error = true, Errors = new Dictionary<string, string>() { { "URL", "Invalid URL" } } };
        if (report.ParsedExtensionVersion == null)
            return new ReportResponse() { Error = true, Errors = new Dictionary<string, string>() { { "ExtensionVersion", "Invalid Extension Version" } } };

        var issueURL = await _githubService.ReportWebsiteAsync(report, browser);
        return new ReportResponse() { ResponseURL = issueURL };
    }
}


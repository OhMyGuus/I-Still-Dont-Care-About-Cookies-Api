using IStillDontCareAboutCookies.Api.Models;

namespace IStillDontCareAboutCookies.Api.Services.Interfaces;

public interface IReportService
{
    Task<ReportResponse> Report(ReportModel report, string browser);
}
using IStillDontCareAboutCookies.Api.Helpers;
using IStillDontCareAboutCookies.Api.Models;
using IStillDontCareAboutCookies.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IStillDontCareAboutCookies.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportController> _logger;

    public ReportController(IReportService reportService, ILogger<ReportController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<ReportResponse>> Post([FromBody] ReportModel report)
    {
        try
        {
            return Ok(await _reportService.Report(report, Request.Headers.GetBrowser()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Something went wrong with reporting the site {URL}", report);
        }
        return Ok(new ReportResponse() { Error = true });
    }
}

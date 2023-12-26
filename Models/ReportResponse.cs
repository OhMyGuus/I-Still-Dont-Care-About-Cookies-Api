namespace IStillDontCareAboutCookies.Api.Models;

public class ReportResponse
{
    public string? ResponseURL { get; set; }
    public bool Error { get; set; } = false;
    public Dictionary<string, string>? Errors { get; set; }

}

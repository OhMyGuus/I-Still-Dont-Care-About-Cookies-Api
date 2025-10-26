namespace IStillDontCareAboutCookies.Api.Services;

public class NSFWCheckerOptions
{
    public const string SectionName = "NSFWChecker";

    public string ListUrl { get; set; } = "https://github.com/badmojr/addons_1Hosts/raw/main/kidSaf/domains.txt";

    public int RefreshIntervalHours { get; set; } = 24;

    public int HttpTimeoutSeconds { get; set; } = 30;
}

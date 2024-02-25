using MyCSharp.HttpUserAgentParser;

namespace IStillDontCareAboutCookies.Api.Helpers;

public static class UserAgentHelper
{
    public static string GetBrowser(this IHeaderDictionary headers)
    {
        string userAgent = headers.UserAgent.ToString();
        HttpUserAgentInformation parsed = HttpUserAgentParser.Parse(userAgent);

        return $"{parsed.Name} {parsed.Version}";
    }
}

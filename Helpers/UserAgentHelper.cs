namespace IStillDontCareAboutCookies.Api.Helpers;
public static class UserAgentHelper
{
    public static string GetBrowser(this IHeaderDictionary headers)
    {
        var parsed = UAParser.Parser.GetDefault().Parse(headers.UserAgent);
        return $"{parsed.UA.Family} {parsed.UA.Major}";
    }

}

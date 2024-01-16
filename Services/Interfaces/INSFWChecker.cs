namespace IStillDontCareAboutCookies.Api.Services.Interfaces;

public interface INSFWChecker
{
    Task<HashSet<string>> FetchList();
    bool IsHostnameNSFW(string hostname);
}
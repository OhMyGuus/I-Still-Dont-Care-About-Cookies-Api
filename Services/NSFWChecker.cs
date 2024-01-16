using IStillDontCareAboutCookies.Api.Services.Interfaces;

namespace IStillDontCareAboutCookies.Api.Services;

public class NSFWChecker : INSFWChecker
{
    private static readonly string _nsfwListUrl = "https://github.com/badmojr/addons_1Hosts/raw/main/kidSaf/domains.txt";

    private static HashSet<string>? _nsfwHostnames;

    public NSFWChecker()
    {
        _nsfwHostnames = FetchList().Result;
    }

    public async Task<HashSet<string>> FetchList()
    {
        using HttpClient httpClient = new();
        var response = await httpClient.GetStringAsync(_nsfwListUrl);

        return new HashSet<string>(response.Split("\n"));
    }

    public bool IsHostnameNSFW(string hostname)
    {
        return _nsfwHostnames?.Contains(hostname) == true;
    }
}
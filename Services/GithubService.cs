using IStillDontCareAboutCookies.Api.Models;
using IStillDontCareAboutCookies.Api.Models.Configuration;
using IStillDontCareAboutCookies.Api.Services.Interfaces;
using Microsoft.Extensions.Options;
using Octokit;

namespace IStillDontCareAboutCookies.Api.Services;

public class GithubService : IGithubService
{
    private readonly GithubConfiguration _githubConfiguration;
    private readonly GitHubClient _githubClient;
    private readonly string _repoOwner = "";
    private readonly string _repoName = "";

    public GithubService(IOptions<GithubConfiguration> githubConfiguration)
    {
        _githubConfiguration = githubConfiguration.Value;
        _githubClient = new GitHubClient(new ProductHeaderValue("IStillDontCareAboutCookiesApi"));
        _githubClient.Credentials = new Credentials(_githubConfiguration.Token);
        _repoName = _githubConfiguration.RepoName;
        _repoOwner = _githubConfiguration.RepoOwner;
    }

    public async Task<string?> ReportWebsiteAsync(ReportModel report, string browser)
    {
        if (report.ParsedExtensionVersion == null)
        {
            return null;
        }
        var existingIssue = await FindExistingIssue(report.Hostname, report.ParsedExtensionVersion);

        if (existingIssue != null)
        {
            await UpdateExistingIssue(existingIssue, report, browser);
            return existingIssue.HtmlUrl;
        }
        else
        {
            return await CreateNewIssue(report, browser);
        }
    }

    private async Task UpdateExistingIssue(Issue existingIssue, ReportModel report, string browser)
    {
        var commentBody = $"Recieved another report for this site:\r\n\r\n ### What browser are u using?\r\n\r\n{browser}\r\n\r\n### Version\r\n\r\n{report.ExtensionVersion}\r\n\r\n### Issue type\r\n\r\n{report.IssueType}\r\n\r\n### Notes\r\n\r\n{report.Notes}";

        await _githubClient.Issue.Comment.Create(_repoOwner, _repoName, existingIssue.Number, commentBody);

        if (existingIssue.Labels.All(o => o.Name != $"V{report.ExtensionVersion}"))
        {
            await _githubClient.Issue.Labels.AddToIssue(_repoOwner, _repoName, existingIssue.Number, [$"V{report.ExtensionVersion}"]);
        }

        if (report.IssueType != IssueType.General)
        {
            await _githubClient.Issue.Labels.AddToIssue(_repoOwner, _repoName, existingIssue.Number, [report.IssueType.ToString()]);
        }
    }

    private async Task<string> CreateNewIssue(ReportModel report, string browser)
    {
        var nsfwChecker = new NSFWChecker();
        bool isNSFW = nsfwChecker.IsHostnameNSFW(report.Hostname);

        var createIssue = new NewIssue($"[REQ] {report.Hostname}");

        if (isNSFW)
            createIssue.Labels.Add("NSFW");

        if (report.IssueType != IssueType.General)
        {
            createIssue.Labels.Add(report.IssueType.ToString());
        }

        createIssue.Body = "Someone reported anonymously: \r\n";
        createIssue.Body += $"### Website URL\r\n\r\n{(isNSFW ? "🔴 NSFW\n" : "")}https://{report.Hostname}\r\n\r\n";
        createIssue.Body += $"### What browser are u using?\r\n\r\n{browser}\r\n\r\n";
        createIssue.Body += $"### Version\r\n\r\n{report.ExtensionVersion}\r\n\r\n";
        createIssue.Body += $"### Issue type\r\n\r\n{report.IssueType}\r\n\r\n";
        createIssue.Body += $"### Notes\r\n\r\n{report.Notes}";

        createIssue.Assignees.Add(_repoOwner);
        createIssue.Labels.Add("Website request");
        createIssue.Labels.Add($"V{report.ExtensionVersion}");
        createIssue.Labels.Add("Anonymous request");

        var issue = await _githubClient.Issue.Create(_repoOwner, _repoName, createIssue);
        return issue.HtmlUrl;
    }

    public async Task<Issue?> FindExistingIssue(string site, Version version)
    {
        var openIssues = await _githubClient.Search.SearchIssues(new SearchIssuesRequest($"repo:{_repoOwner}/{_repoName} {site} is:issue is:open"));

        if (openIssues.Items.Any())
        {
            return openIssues.Items.FirstOrDefault();
        }
        else
        {
            var closedIssues = await _githubClient.Search.SearchIssues(new SearchIssuesRequest($"repo:{_repoOwner}/{_repoName} {site} is:issue is:closed"));
            return closedIssues.Items.LastOrDefault(o => IssueVersionCheck(o, version));
        }
    }

    private static bool IssueVersionCheck(Issue issue, Version currentVersion)
    {
        return issue.Labels.Any(label =>
        {
            var labelVersionString = label.Name.TrimStart('V');
            return label.Name == $"V{currentVersion}" ||
                   (Version.TryParse(labelVersionString, out var labelVersion) &&
                    labelVersion >= currentVersion);
        });
    }


}

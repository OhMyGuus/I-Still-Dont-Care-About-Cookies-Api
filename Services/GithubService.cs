using IStillDontCareAboutCookies.Api.Models;
using IStillDontCareAboutCookies.Api.Models.Configuration;
using IStillDontCareAboutCookies.Api.Services.Interfaces;
using Microsoft.Extensions.Options;
using Octokit;
using GitHubJwt;

namespace IStillDontCareAboutCookies.Api.Services;

public class GithubService : IGithubService
{
    private readonly GithubConfiguration _githubConfiguration;
    private readonly GitHubClient _githubClient;
    private readonly INSFWChecker _nsfwChecker;
    private readonly string _repoOwner = "";
    private readonly string _repoName = "";
    private readonly bool _useGitHubApp;
    private readonly SemaphoreSlim _tokenRefreshLock = new(1, 1);
    private DateTimeOffset _tokenExpiresAt = DateTimeOffset.MinValue;

    public GithubService(IOptions<GithubConfiguration> githubConfiguration, INSFWChecker nsfwChecker)
    {
        _githubConfiguration = githubConfiguration.Value;
        _nsfwChecker = nsfwChecker;
        _githubClient = new GitHubClient(new ProductHeaderValue("IStillDontCareAboutCookiesApi"));

        // Determine authentication method
        _useGitHubApp = _githubConfiguration.AppId.HasValue &&
                        !string.IsNullOrEmpty(_githubConfiguration.PrivateKey) &&
                        _githubConfiguration.InstallationId.HasValue;

        if (_useGitHubApp)
        {
            // Initial token will be generated on first API call
            RefreshGitHubAppCredentialsAsync().Wait();
        }
        else
        {
            _githubClient.Credentials = new Credentials(_githubConfiguration.Token);
        }

        _repoName = _githubConfiguration.RepoName;
        _repoOwner = _githubConfiguration.RepoOwner;
    }

    private async Task EnsureValidTokenAsync()
    {
        if (!_useGitHubApp)
            return;

        // Refresh token if it expires in less than 5 minutes
        if (DateTimeOffset.UtcNow.AddMinutes(5) >= _tokenExpiresAt)
        {
            await RefreshGitHubAppCredentialsAsync();
        }
    }

    private async Task RefreshGitHubAppCredentialsAsync()
    {
        await _tokenRefreshLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (DateTimeOffset.UtcNow.AddMinutes(5) < _tokenExpiresAt)
                return;

            // Generate JWT token for GitHub App authentication
            var generator = new GitHubJwtFactory(
                new StringPrivateKeySource(_githubConfiguration.PrivateKey!),
                new GitHubJwtFactoryOptions
                {
                    AppIntegrationId = _githubConfiguration.AppId!.Value,
                    ExpirationSeconds = 600 // 10 minutes (maximum allowed)
                }
            );

            var jwtToken = generator.CreateEncodedJwtToken();

            // Create a client authenticated as the GitHub App
            var appClient = new GitHubClient(new ProductHeaderValue("IStillDontCareAboutCookiesApi"))
            {
                Credentials = new Credentials(jwtToken, AuthenticationType.Bearer)
            };

            // Create installation token
            var installationToken = await appClient.GitHubApps.CreateInstallationToken(_githubConfiguration.InstallationId!.Value);

            // Update credentials and expiration time
            _githubClient.Credentials = new Credentials(installationToken.Token);
            _tokenExpiresAt = installationToken.ExpiresAt;
        }
        finally
        {
            _tokenRefreshLock.Release();
        }
    }

    public async Task<string?> ReportWebsiteAsync(ReportModel report, string browser)
    {
        await EnsureValidTokenAsync();

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
        bool isNSFW = _nsfwChecker.IsHostnameNSFW(report.Hostname);

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
        await EnsureValidTokenAsync();

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

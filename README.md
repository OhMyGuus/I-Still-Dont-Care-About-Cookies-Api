<div align="center">

<image src="docs/images/extension-icon-128.png">

# I still don't care about cookies API

</div>

This is the ASP.NET API for the _[I Still Dont Care About Cookies](https://github.com/OhMyGuus/I-Still-Dont-Care-About-Cookies)_ extension. It uses .NET 8.0 as a target framework, and also makes use of [Octokit](https://github.com/octokit/octokit.net), [Swashbuckle](https://github.com/domaindrivendev/Swashbuckle.AspNetCore) and [UAParser](https://github.com/ua-parser/uap-csharp) as package dependencies. Currently, this API is used for anonymous reports.

## Building
### Linux (Arch)
As this requires .NET 8.0 and the ASP.NET runtime, you will need to install these packages from the AUR. You can use `pacman` or `yay` for this - following the commands below:

_Install the .NET SDK and ASP.NET runtime_
```
pacman -S dotnet-sdk aspnet-runtime
```
_or_
```bash
yay -S dotnet-sdk aspnet-runtime
```

And that's it! Theoretically, you should be ready to go now. If you type `dotnet run` within the project directory, it will build and run the API. You can verify that it's running properly by checking the console;
```
Now listening on: http://localhost:5018
```
If you see this, all is well and the API server is live!

## Development
To be able to work on the API, you will need to [generate a personal GitHub API token](https://github.com/settings/tokens?type=beta) and make sure to select `Issues` as an accepted permission of this token so you can programmatically do stuff with GitHub issues (which is a feature of the API).

It's also worth noting that your fork of the ISDCAC extension may have the _Issues_ tab disabled by default; you can fix this by clicking on the _Settings_ tab of your fork and checking the _Issues_ box.

If you use VSCode/VSCodium/VS, we recommend installing the [.NET Core User Secrets Extension](https://marketplace.visualstudio.com/items?itemName=adrianwilczynski.user-secrets). This is so you don't accidentally push your GitHub API token. The setup for this extension is super simple; after installing it, right click the `IStillDontCareAboutCookies.Api.csproj` file in your IDE and click `Manage User Secrets` which will be at the top of your context menu. An empty JSON file called `secrets.json` should appear, replace that file with this content (in full):

```json
{
    "GithubConfiguration": {
        "RepoOwner": "Your GitHub username here",
        "RepoName": "I-Still-Dont-Care-About-Cookies",
        "Token": "Your personal GitHub token "
    }
}
```

If you forked the extension under a different name, then replace the `RepoName` key with that custom name. Hit save and then this `secrets.json` file will replace the `GithubConfiguration` dictionary at runtime, avoiding you pushing your token by accident!

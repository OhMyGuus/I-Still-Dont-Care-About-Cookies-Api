using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace IStillDontCareAboutCookies.Api.Models;

public class ReportModel
{
    private string? _extensionVersion;

    [Required]
    public required string URL { get; set; }

    public IssueType IssueType { get; set; }

    public string? Notes { get; set; }

    [Required]
    public required string ExtensionVersion
    {
        get => _extensionVersion!;
        set
        {
            _extensionVersion = value;
            ParsedExtensionVersion = Version.TryParse(value, out var result) ? result : null;
        }
    }

    [JsonIgnore]
    public Version? ParsedExtensionVersion { get; private set; }

    public string Hostname => GetUri()?.Host ?? URL ?? string.Empty;

    public Uri? GetUri()
    {
        return Uri.TryCreate(URL, UriKind.Absolute, out var uri) ? uri : null;
    }
}


public enum IssueType
{
    General,
    Modal,
    Scrollbar
}

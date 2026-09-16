using System.Text.Json.Serialization;

namespace TokaZerkUIConfig.Infrastructure;

internal sealed class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string? TagName { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("assets")]
    public GitHubAsset[]? Assets { get; set; }

    [JsonPropertyName("draft")]
    public bool Draft { get; set; }
}

internal sealed class GitHubAsset
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("browser_download_url")]
    public string? BrowserDownloadUrl { get; set; }
}

[JsonSerializable(typeof(GitHubRelease))]
[JsonSerializable(typeof(GitHubRelease[]))]
internal partial class GitHubReleaseJsonContext : JsonSerializerContext
{
}

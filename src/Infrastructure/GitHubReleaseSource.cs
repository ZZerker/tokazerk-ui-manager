using System.IO.Abstractions;
using System.Net.Http.Headers;
using System.Text.Json;
using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Domain.Ports;

namespace TokaZerkUIConfig.Infrastructure;

public sealed class GitHubReleaseSource(HttpClient httpClient, IFileSystem fileSystem) : IReleaseSource
{
    private const int COPY_BUFFER_SIZE = 81920;

    public async Task<ReleaseInfo?> GetLatestAsync(string owner, string repo, Func<string, bool> assetFilter, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{owner}/{repo}/releases/latest");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        // GitHub rejects requests without a User-Agent header.
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("TokaZerkUIConfig", null));

        using var response = await httpClient.SendAsync(request, ct).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var release = await JsonSerializer.DeserializeAsync(
            await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false),
            GitHubReleaseJsonContext.Default.GitHubRelease,
            ct).ConfigureAwait(false);

        if (release is null)
        {
            return null;
        }

        var asset = release.Assets?.FirstOrDefault(a => a.Name is not null && assetFilter(a.Name));
        if (asset is null || asset.Name is null || asset.BrowserDownloadUrl is null)
        {
            return null;
        }

        if (!SemVer.TryParse(release.TagName, out var version))
        {
            return null;
        }

        return new ReleaseInfo(version, release.TagName!, asset.Name, asset.BrowserDownloadUrl, release.Body ?? "");
    }

    public async Task DownloadAsync(string url, string destinationFile, IProgress<double>? progress, CancellationToken ct)
    {
        using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var directory = fileSystem.Path.GetDirectoryName(destinationFile);
        if (!string.IsNullOrEmpty(directory))
        {
            fileSystem.Directory.CreateDirectory(directory);
        }

        var contentLength = response.Content.Headers.ContentLength;

        await using var sourceStream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        await using var destinationStream = fileSystem.File.Create(destinationFile);

        var buffer = new byte[COPY_BUFFER_SIZE];
        long totalRead = 0;
        int bytesRead;
        while ((bytesRead = await sourceStream.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
        {
            await destinationStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct).ConfigureAwait(false);
            totalRead += bytesRead;
            if (contentLength.HasValue && contentLength.Value > 0)
            {
                progress?.Report(totalRead / (double)contentLength.Value);
            }
        }

        progress?.Report(1.0);
    }
}

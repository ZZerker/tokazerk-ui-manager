using System.IO.Abstractions;
using System.Net.Http.Headers;
using System.Text.Json;
using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Domain.Ports;

namespace TokaZerkUIConfig.Infrastructure;

public sealed class GitHubReleaseSource(HttpClient httpClient, IFileSystem fileSystem) : IReleaseSource
{
    private const int COPY_BUFFER_SIZE = 81920;
    private const int PAGE_SIZE = 20;

    public async Task<ReleaseInfo?> GetLatestAsync(string owner, string repo, bool includePreReleases, Func<string, bool> assetFilter, CancellationToken ct)
    {
        if (!includePreReleases)
        {
            using var response = await SendAsync($"https://api.github.com/repos/{owner}/{repo}/releases/latest", ct).ConfigureAwait(false);
            if (response is null)
            {
                return null;
            }

            var release = await JsonSerializer.DeserializeAsync(
                await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false),
                GitHubReleaseJsonContext.Default.GitHubRelease,
                ct).ConfigureAwait(false);

            return release is null ? null : ToReleaseInfo(release, assetFilter);
        }

        // /releases/latest never returns pre-releases, hence the list on the beta path.
        using var listResponse = await SendAsync($"https://api.github.com/repos/{owner}/{repo}/releases?per_page={PAGE_SIZE}", ct).ConfigureAwait(false);
        if (listResponse is null)
        {
            return null;
        }

        var releases = await JsonSerializer.DeserializeAsync(
            await listResponse.Content.ReadAsStreamAsync(ct).ConfigureAwait(false),
            GitHubReleaseJsonContext.Default.GitHubReleaseArray,
            ct).ConfigureAwait(false);

        if (releases is null)
        {
            return null;
        }

        return releases
            .Where(r => !r.Draft)
            .Select(r => ToReleaseInfo(r, assetFilter))
            .Where(r => r is not null)
            .MaxBy(r => r!.Version);
    }

    private async Task<HttpResponseMessage?> SendAsync(string url, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        // GitHub rejects requests without a User-Agent header.
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("TokaZerkUIConfig", null));

        var response = await httpClient.SendAsync(request, ct).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            response.Dispose();
            return null;
        }

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch
        {
            response.Dispose();
            throw;
        }

        return response;
    }

    private static ReleaseInfo? ToReleaseInfo(GitHubRelease release, Func<string, bool> assetFilter)
    {
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

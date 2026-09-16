using System.IO.Abstractions.TestingHelpers;
using System.Net;
using System.Text;
using TokaZerkUIConfig.Infrastructure;
using Xunit;

namespace TokaZerkUIConfig.Infrastructure.Tests;

public class GitHubReleaseSourceTests
{
    private const string ReleaseJson = """
        {
          "tag_name": "v1.0.12",
          "body": "notes",
          "assets": [
            { "name": "TokaZerkUI-v1.0.12.zip", "browser_download_url": "https://example.com/TokaZerkUI-v1.0.12.zip" },
            { "name": "TokaZerkUIConfig-win-x64.exe", "browser_download_url": "https://example.com/TokaZerkUIConfig-win-x64.exe" },
            { "name": "TokaZerkUIConfig-linux-x64", "browser_download_url": "https://example.com/TokaZerkUIConfig-linux-x64" }
          ]
        }
        """;

    [Fact]
    public async Task GetLatestAsyncParsesReleaseJsonAndPicksMatchingAsset()
    {
        var handler = new StubHandler(HttpStatusCode.OK, ReleaseJson);
        var source = new GitHubReleaseSource(new HttpClient(handler), new MockFileSystem());

        var release = await source.GetLatestAsync("tokajer", "TokaZerkUI", n => n.EndsWith(".zip"), CancellationToken.None);

        Assert.NotNull(release);
        Assert.Equal(new TokaZerkUIConfig.Domain.SemVer(1, 0, 12), release!.Version);
        Assert.Equal("v1.0.12", release.Tag);
        Assert.Equal("TokaZerkUI-v1.0.12.zip", release.AssetName);
        Assert.Equal("https://example.com/TokaZerkUI-v1.0.12.zip", release.AssetUrl);
        Assert.Equal("notes", release.Notes);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal("https://api.github.com/repos/tokajer/TokaZerkUI/releases/latest", handler.LastRequest!.RequestUri!.ToString());
        Assert.True(handler.LastRequest.Headers.UserAgent.Count > 0);
    }

    [Fact]
    public async Task GetLatestAsyncPicksAssetMatchingRidFilter()
    {
        var handler = new StubHandler(HttpStatusCode.OK, ReleaseJson);
        var source = new GitHubReleaseSource(new HttpClient(handler), new MockFileSystem());

        var release = await source.GetLatestAsync("tokajer", "TokaZerkUI", n => n.Contains("linux-x64"), CancellationToken.None);

        Assert.NotNull(release);
        Assert.Equal("TokaZerkUIConfig-linux-x64", release!.AssetName);
        Assert.Equal("https://example.com/TokaZerkUIConfig-linux-x64", release.AssetUrl);
    }

    [Fact]
    public async Task GetLatestAsyncReturnsNullWhenNoAssetMatchesFilter()
    {
        var handler = new StubHandler(HttpStatusCode.OK, ReleaseJson);
        var source = new GitHubReleaseSource(new HttpClient(handler), new MockFileSystem());

        var release = await source.GetLatestAsync("tokajer", "TokaZerkUI", n => n.EndsWith(".dmg"), CancellationToken.None);

        Assert.Null(release);
    }

    [Fact]
    public async Task GetLatestAsyncReturnsNullOn404()
    {
        var handler = new StubHandler(HttpStatusCode.NotFound, "");
        var source = new GitHubReleaseSource(new HttpClient(handler), new MockFileSystem());

        var release = await source.GetLatestAsync("tokajer", "TokaZerkUI", _ => true, CancellationToken.None);

        Assert.Null(release);
    }

    [Fact]
    public async Task GetLatestAsyncThrowsOnServerError()
    {
        var handler = new StubHandler(HttpStatusCode.InternalServerError, "boom");
        var source = new GitHubReleaseSource(new HttpClient(handler), new MockFileSystem());

        await Assert.ThrowsAsync<HttpRequestException>(() => source.GetLatestAsync("tokajer", "TokaZerkUI", n => true, CancellationToken.None));
    }

    [Fact]
    public async Task DownloadAsyncWritesBytesAndReportsCompletion()
    {
        var bytes = Encoding.UTF8.GetBytes("payload-contents");
        var handler = new StubHandler(HttpStatusCode.OK, bytes);
        var fileSystem = new MockFileSystem();
        var destination = fileSystem.Path.Combine("downloads", "nested", "file.zip");
        var source = new GitHubReleaseSource(new HttpClient(handler), fileSystem);
        var reports = new List<double>();
        // Progress<T> posts callbacks asynchronously, so a synchronous reporter keeps the assertion deterministic.
        var progress = new SyncProgress(reports.Add);

        await source.DownloadAsync("https://example.com/file.zip", destination, progress, CancellationToken.None);

        Assert.True(fileSystem.File.Exists(destination));
        Assert.Equal(bytes, fileSystem.File.ReadAllBytes(destination));
        Assert.Equal(1.0, reports[^1]);
    }

    private sealed class SyncProgress(Action<double> report) : IProgress<double>
    {
        public void Report(double value) => report(value);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode statusCode;
        private readonly byte[] content;

        public StubHandler(HttpStatusCode statusCode, string content)
            : this(statusCode, Encoding.UTF8.GetBytes(content))
        {
        }

        public StubHandler(HttpStatusCode statusCode, byte[] content)
        {
            this.statusCode = statusCode;
            this.content = content;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.LastRequest = request;
            var response = new HttpResponseMessage(this.statusCode)
            {
                Content = new ByteArrayContent(this.content),
            };
            return Task.FromResult(response);
        }
    }
}

using TokaZerkUIConfig.Domain;
using Xunit;

namespace TokaZerkUIConfig.Domain.Tests;

public class ReleaseRepositoriesTests
{
    [Fact]
    public void IsToolAsset_MatchesWinX64ExeName()
    {
        Assert.True(ReleaseRepositories.IsToolAsset("TokaZerkUIConfig-win-x64.exe", "win-x64"));
    }

    [Fact]
    public void IsToolAsset_MatchesLinuxX64Name()
    {
        Assert.True(ReleaseRepositories.IsToolAsset("TokaZerkUIConfig-linux-x64", "linux-x64"));
    }

    [Fact]
    public void IsToolAsset_DoesNotMatchLinuxX64MuslAgainstLinuxX64()
    {
        Assert.False(ReleaseRepositories.IsToolAsset("TokaZerkUIConfig-linux-x64-musl", "linux-x64"));
    }

    [Fact]
    public void IsUiAsset_AcceptsTokaZerkUiZip()
    {
        Assert.True(ReleaseRepositories.IsUiAsset("TokaZerkUI-v1.0.12.zip"));
    }

    [Fact]
    public void IsUiAsset_RejectsToolExe()
    {
        Assert.False(ReleaseRepositories.IsUiAsset("TokaZerkUIConfig-win-x64.exe"));
    }
}

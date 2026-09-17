using TokaZerkUIConfig.Domain;
using Xunit;

namespace TokaZerkUIConfig.Domain.Tests;

public class ReleaseRepositoriesTests
{
    [Fact]
    public void ToolRepositoryUsesZerkerOwner()
    {
        Assert.Equal("ZZerker", ReleaseRepositories.TOOL_OWNER);
        Assert.Equal("tokazerk-ui-manager", ReleaseRepositories.TOOL_REPO);
    }

    [Fact]
    public void IsToolAsset_MatchesWinX64ExeName()
    {
        Assert.True(ReleaseRepositories.IsToolAsset("TokaZerkUIManager-win-x64.exe", "win-x64"));
    }

    [Fact]
    public void IsToolAsset_MatchesLinuxX64Name()
    {
        Assert.True(ReleaseRepositories.IsToolAsset("TokaZerkUIManager-linux-x64", "linux-x64"));
    }

    [Fact]
    public void IsToolAsset_DoesNotMatchLinuxX64MuslAgainstLinuxX64()
    {
        Assert.False(ReleaseRepositories.IsToolAsset("TokaZerkUIManager-linux-x64-musl", "linux-x64"));
    }

    [Fact]
    public void IsToolAsset_DoesNotMatchLinuxX64Exe()
    {
        Assert.False(ReleaseRepositories.IsToolAsset("TokaZerkUIManager-linux-x64.exe", "linux-x64"));
    }

    [Fact]
    public void IsUiAsset_AcceptsTokaZerkUiZip()
    {
        Assert.True(ReleaseRepositories.IsUiAsset("TokaZerkUI-v1.0.12.zip"));
    }

    [Fact]
    public void IsUiAsset_RejectsToolExe()
    {
        Assert.False(ReleaseRepositories.IsUiAsset("TokaZerkUIManager-win-x64.exe"));
    }
}

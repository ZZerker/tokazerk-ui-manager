using TokaZerkUIConfig.Domain;
using Xunit;

namespace TokaZerkUIConfig.Domain.Tests;

public class SemVerTests
{
    [Theory]
    [InlineData("1.0.11", 1, 0, 11, 0)]
    [InlineData("v1.0.11", 1, 0, 11, 0)]
    [InlineData("1.2", 1, 2, 0, 0)]
    [InlineData("1.2.3.4", 1, 2, 3, 4)]
    public void TryParse_Succeeds(string text, int major, int minor, int patch, int build)
    {
        Assert.True(SemVer.TryParse(text, out var version));
        Assert.Equal(new SemVer(major, minor, patch, build), version);
    }

    [Fact]
    public void TryParse_Succeeds_WithPreRelease()
    {
        Assert.True(SemVer.TryParse("v1.0.12-beta.3", out var version));
        Assert.Equal(new SemVer(1, 0, 12, 0, 3), version);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a version")]
    [InlineData("1.2.3-beta")]
    [InlineData("1.2.3-rc.1")]
    [InlineData("1.2.3-beta.1x")]
    [InlineData("1.0.12-beta.99999999999999999999")]
    public void TryParse_Fails(string? text)
    {
        Assert.False(SemVer.TryParse(text, out _));
    }

    [Fact]
    public void Compare_ReleaseOutranksItsBetas()
    {
        Assert.True(new SemVer(1, 0, 12, 0, 2) < new SemVer(1, 0, 12));
        Assert.True(new SemVer(1, 0, 12, 0, 1) < new SemVer(1, 0, 12, 0, 2));
        Assert.True(new SemVer(1, 0, 12, 0, 9) > new SemVer(1, 0, 11));
    }

    [Fact]
    public void Compare_OrdersByMajorMinorPatchBuild()
    {
        Assert.True(new SemVer(1, 0, 0) < new SemVer(1, 0, 1));
        Assert.True(new SemVer(1, 1, 0) > new SemVer(1, 0, 9));
        Assert.True(new SemVer(1, 0, 0, 1) > new SemVer(1, 0, 0));
    }

    [Fact]
    public void ToString_OmitsBuild_WhenZero()
    {
        Assert.Equal("1.0.11", new SemVer(1, 0, 11).ToString());
        Assert.Equal("1.0.11.2", new SemVer(1, 0, 11, 2).ToString());
    }

    [Fact]
    public void ToString_RoundTrips_PreRelease()
    {
        Assert.Equal("1.0.12-beta.1", new SemVer(1, 0, 12, 0, 1).ToString());
    }

    [Fact]
    public void FindInText_FindsFirstVersionInVersionInfoLine()
    {
        var version = SemVer.FindInText("Tokajer's and Zerkers DAOC UI Eden 1.0.11");

        Assert.Equal(new SemVer(1, 0, 11), version);
    }

    [Fact]
    public void FindInText_ReturnsNull_WhenNoVersion()
    {
        Assert.Null(SemVer.FindInText("no version here"));
    }
}

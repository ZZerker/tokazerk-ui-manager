using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Domain.Ports;
using TokaZerkUIConfig.Infrastructure;
using Xunit;

namespace TokaZerkUIConfig.Infrastructure.Tests;

internal static class RealPackageLocator
{
    public static string ResolvePath()
    {
        var fromEnv = Environment.GetEnvironmentVariable("TOKAZERK_UI_PACKAGE");
        return string.IsNullOrEmpty(fromEnv) ? @"D:\PrivatProjects\tokajerui.git" : fromEnv;
    }

    public static bool IsAvailable()
    {
        return File.Exists(Path.Combine(ResolvePath(), "assets.xml"));
    }
}

public sealed class RealPackageFactAttribute : FactAttribute
{
    public RealPackageFactAttribute()
    {
        if (!RealPackageLocator.IsAvailable())
        {
            this.Skip = "Set TOKAZERK_UI_PACKAGE to a TokaZerk UI folder";
        }
    }
}

public sealed class RealPackageTheoryAttribute : TheoryAttribute
{
    public RealPackageTheoryAttribute()
    {
        if (!RealPackageLocator.IsAvailable())
        {
            this.Skip = "Set TOKAZERK_UI_PACKAGE to a TokaZerk UI folder";
        }
    }
}

public class ForgePreviewRendererTests
{
    // stats_group draws with TokaSmall only and chat with TokaLarge only, so each tier is
    // checked on a window that actually uses it.
    [RealPackageTheory]
    [InlineData("stats_group", FontTier.Small, 10, 14)]
    [InlineData("chat", FontTier.Large, 12, 16)]
    public async Task RenderAsync_TierChanged_ProducesDifferentPngsWithoutFailures(string windowId, FontTier tier, int firstPx, int secondPx)
    {
        var path = RealPackageLocator.ResolvePath();
        using var renderer = new ForgePreviewRenderer();

        var first = await renderer.RenderAsync(path, windowId, FontSettings.Default.With(tier, firstPx), CancellationToken.None);
        var second = await renderer.RenderAsync(path, windowId, FontSettings.Default.With(tier, secondPx), CancellationToken.None);

        Assert.Equal(0, first.FailureCount);
        Assert.Equal(0, second.FailureCount);
        Assert.True(first.Width > 0);
        Assert.True(second.Width > 0);
        Assert.NotEqual(first.PngBytes, second.PngBytes);
    }

    [RealPackageFact]
    public async Task RenderAsync_UnknownWindow_Throws()
    {
        var path = RealPackageLocator.ResolvePath();
        using var renderer = new ForgePreviewRenderer();

        await Assert.ThrowsAsync<PreviewWindowNotFoundException>(
            () => renderer.RenderAsync(path, "does_not_exist", FontSettings.Default, CancellationToken.None));
    }
}

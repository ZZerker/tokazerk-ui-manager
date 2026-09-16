using TokaZerkUIConfig.Domain;
using Xunit;

namespace TokaZerkUIConfig.Domain.Tests;

public class VariantTableTests
{
    [Fact]
    public void All_HasExactlyThreeVariants()
    {
        Assert.Equal(3, VariantTable.All.Count);
    }

    [Fact]
    public void MapSize_HasExpectedShape()
    {
        var variant = VariantTable.Get(VariantKind.MapSize);

        Assert.Equal("Maps", variant.TargetPath);
        Assert.Equal(["Warmap", "regions.dat"], variant.Preserve);
        Assert.Equal(3, variant.Choices.Count);
        Assert.Contains(variant.Choices, c => c is { Id: "large", SourcePath: "Maps_large" });
        Assert.Contains(variant.Choices, c => c is { Id: "small", SourcePath: "Maps_small" });
        Assert.Contains(variant.Choices, c => c is { Id: "default", SourcePath: null });
    }

    [Fact]
    public void TargetWindow_HasExpectedShape()
    {
        var variant = VariantTable.Get(VariantKind.TargetWindow);

        Assert.Equal("custom2_window.xml", variant.TargetPath);
        Assert.Empty(variant.Preserve);
        Assert.Contains(variant.Choices, c => c is { Id: "default", SourcePath: "Options/TargetWindow/blue(TokaZerk)/custom2_window.xml", Label: "Blue (TokaZerk)" });
        Assert.Contains(variant.Choices, c => c is { Id: "purple", SourcePath: "Options/TargetWindow/purple/custom2_window.xml" });
    }

    [Fact]
    public void FloatTarget_HasExpectedShape()
    {
        var variant = VariantTable.Get(VariantKind.FloatTarget);

        Assert.Equal("float_target_window.xml", variant.TargetPath);
        Assert.Contains(variant.Choices, c => c is { Id: "default", SourcePath: null });
        Assert.Contains(variant.Choices, c => c is { Id: "hud", SourcePath: "Options/floatTargetWindow/float_target_window.xml" });
    }
}

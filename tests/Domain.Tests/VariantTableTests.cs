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
        Assert.Equal(new[] { "Warmap", "regions.dat" }, variant.Preserve);
        Assert.Equal(3, variant.Choices.Count);
        Assert.Contains(variant.Choices, c => c.Id == "large" && c.SourcePath == "Maps_large");
        Assert.Contains(variant.Choices, c => c.Id == "small" && c.SourcePath == "Maps_small");
        Assert.Contains(variant.Choices, c => c.Id == "default" && c.SourcePath == null);
    }

    [Fact]
    public void TargetWindow_HasExpectedShape()
    {
        var variant = VariantTable.Get(VariantKind.TargetWindow);

        Assert.Equal("custom2_window.xml", variant.TargetPath);
        Assert.Empty(variant.Preserve);
        Assert.Contains(variant.Choices, c => c.Id == "default" && c.SourcePath == "Options/TargetWindow/blue(TokaZerk)/custom2_window.xml" && c.Label == "Blue (TokaZerk)");
        Assert.Contains(variant.Choices, c => c.Id == "purple" && c.SourcePath == "Options/TargetWindow/purple/custom2_window.xml");
    }

    [Fact]
    public void FloatTarget_HasExpectedShape()
    {
        var variant = VariantTable.Get(VariantKind.FloatTarget);

        Assert.Equal("float_target_window.xml", variant.TargetPath);
        Assert.Contains(variant.Choices, c => c.Id == "default" && c.SourcePath == null);
        Assert.Contains(variant.Choices, c => c.Id == "hud" && c.SourcePath == "Options/floatTargetWindow/float_target_window.xml");
    }
}

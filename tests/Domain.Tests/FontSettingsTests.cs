using TokaZerkUIConfig.Domain;
using Xunit;

namespace TokaZerkUIConfig.Domain.Tests;

public class FontSettingsTests
{
    [Fact]
    public void Default_HasNoValidationWarnings()
    {
        Assert.Empty(FontSettings.Default.Validate());
    }

    [Fact]
    public void Validate_ReportsTierOrderError_WhenOutOfOrder()
    {
        var settings = FontSettings.Default with { Medium = 9 };

        var warnings = settings.Validate();

        Assert.Contains(warnings, w => w is { Kind: FontWarningKind.TierOrder, IsError: true });
    }

    [Theory]
    [InlineData(14, false)]
    [InlineData(15, true)]
    public void Validate_WarnsAboveDefaultPlusTwo_ForUiTier(int px, bool expectWarning)
    {
        var settings = FontSettings.Default with { Large = px };

        var warnings = settings.Validate();

        var hasClipWarning = warnings.Any(w => w is { Tier: FontTier.Large, Kind: FontWarningKind.MayClip });
        Assert.Equal(expectWarning, hasClipWarning);
    }

    [Fact]
    public void Validate_DoesNotWarnForChatTiers()
    {
        var settings = FontSettings.Default with { ChatLarge = 20 };

        var warnings = settings.Validate();

        Assert.DoesNotContain(warnings, w => w is { Tier: FontTier.ChatLarge, Kind: FontWarningKind.MayClip });
    }

    [Theory]
    [InlineData(5)]
    [InlineData(25)]
    public void Validate_ReportsOutOfRangeError(int px)
    {
        var settings = FontSettings.Default with { Small = px };

        var warnings = settings.Validate();

        Assert.Contains(warnings, w => w is { Tier: FontTier.Small, Kind: FontWarningKind.OutOfRange, IsError: true });
    }

    [Fact]
    public void With_And_Get_RoundTrip()
    {
        var settings = FontSettings.Default.With(FontTier.XLarge, 16);

        Assert.Equal(16, settings.Get(FontTier.XLarge));
    }
}

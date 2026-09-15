namespace TokaZerkUIConfig.Domain;

public sealed record FontSettings(int Small, int Medium, int Large, int XLarge, int ChatSmall, int ChatLarge)
{
    public static FontSettings Default { get; } = new(
        FontTierInfo.DefaultPx(FontTier.Small),
        FontTierInfo.DefaultPx(FontTier.Medium),
        FontTierInfo.DefaultPx(FontTier.Large),
        FontTierInfo.DefaultPx(FontTier.XLarge),
        FontTierInfo.DefaultPx(FontTier.ChatSmall),
        FontTierInfo.DefaultPx(FontTier.ChatLarge));

    public int Get(FontTier tier) => tier switch
    {
        FontTier.Small => Small,
        FontTier.Medium => Medium,
        FontTier.Large => Large,
        FontTier.XLarge => XLarge,
        FontTier.ChatSmall => ChatSmall,
        FontTier.ChatLarge => ChatLarge,
        _ => throw new ArgumentOutOfRangeException(nameof(tier)),
    };

    public FontSettings With(FontTier tier, int px) => tier switch
    {
        FontTier.Small => this with { Small = px },
        FontTier.Medium => this with { Medium = px },
        FontTier.Large => this with { Large = px },
        FontTier.XLarge => this with { XLarge = px },
        FontTier.ChatSmall => this with { ChatSmall = px },
        FontTier.ChatLarge => this with { ChatLarge = px },
        _ => throw new ArgumentOutOfRangeException(nameof(tier)),
    };

    public IReadOnlyList<FontWarning> Validate()
    {
        var warnings = new List<FontWarning>();

        if (!(Small <= Medium && Medium <= Large && Large <= XLarge))
        {
            warnings.Add(new FontWarning(null, FontWarningKind.TierOrder, true,
                "Font sizes must satisfy Small <= Medium <= Large <= XLarge."));
        }

        foreach (var tier in Enum.GetValues<FontTier>())
        {
            var px = Get(tier);
            if (px < FontTierInfo.MinPx || px > FontTierInfo.MaxPx)
            {
                warnings.Add(new FontWarning(tier, FontWarningKind.OutOfRange, true,
                    $"{tier} must be between {FontTierInfo.MinPx} and {FontTierInfo.MaxPx} px."));
            }

            if (FontTierInfo.HasClippingLimit(tier) && px > FontTierInfo.DefaultPx(tier) + FontTierInfo.ClippingMarginPx)
            {
                warnings.Add(new FontWarning(tier, FontWarningKind.MayClip, false,
                    $"{tier} above default +{FontTierInfo.ClippingMarginPx}px may clip in fixed label boxes."));
            }
        }

        return warnings;
    }
}

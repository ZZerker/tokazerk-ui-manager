namespace TokaZerkUIConfig.Domain;

public static class FontTierInfo
{
    public const int ClippingMarginPx = 2;
    public const int MinPx = 6;
    public const int MaxPx = 24;

    public static int DefaultPx(FontTier tier) => tier switch
    {
        FontTier.Small => 10,
        FontTier.Medium => 11,
        FontTier.Large => 12,
        FontTier.XLarge => 14,
        FontTier.ChatSmall => 10,
        FontTier.ChatLarge => 13,
        _ => throw new ArgumentOutOfRangeException(nameof(tier)),
    };

    private static readonly IReadOnlyList<string> SmallNames = new[] { "TokaSmall", "TokaSmallBold" };
    private static readonly IReadOnlyList<string> MediumNames = new[] { "TokaMedium", "TokaMediumBold" };
    private static readonly IReadOnlyList<string> LargeNames = new[] { "TokaLarge", "TokaLargeBold" };
    private static readonly IReadOnlyList<string> XLargeNames = new[] { "TokaXLargeBold" };
    private static readonly IReadOnlyList<string> ChatSmallNames = new[] { "chat_small" };
    private static readonly IReadOnlyList<string> ChatLargeNames = new[] { "chat_large" };

    public static IReadOnlyList<string> DefinitionNames(FontTier tier) => tier switch
    {
        FontTier.Small => SmallNames,
        FontTier.Medium => MediumNames,
        FontTier.Large => LargeNames,
        FontTier.XLarge => XLargeNames,
        FontTier.ChatSmall => ChatSmallNames,
        FontTier.ChatLarge => ChatLargeNames,
        _ => throw new ArgumentOutOfRangeException(nameof(tier)),
    };

    public static bool HasClippingLimit(FontTier tier) => tier switch
    {
        FontTier.Small or FontTier.Medium or FontTier.Large or FontTier.XLarge => true,
        FontTier.ChatSmall or FontTier.ChatLarge => false,
        _ => throw new ArgumentOutOfRangeException(nameof(tier)),
    };
}

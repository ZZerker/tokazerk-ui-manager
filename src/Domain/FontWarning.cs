namespace TokaZerkUIConfig.Domain;

public enum FontWarningKind
{
    TierOrder,
    OutOfRange,
    MayClip,
}

public sealed record FontWarning(FontTier? Tier, FontWarningKind Kind, bool IsError, string Message);

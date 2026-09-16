namespace TokaZerkUIConfig.Domain;

public sealed record UiSettings(FontSettings Fonts, VariantSelection Variants)
{
    public static UiSettings Default { get; } = new(FontSettings.Default, new VariantSelection());
}

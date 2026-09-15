namespace TokaZerkUIConfig.Domain;

public sealed record UiSettings(FontSettings Fonts, VariantSelection Variants, SemVer? InstalledUiVersion, bool CheckForUpdatesOnStart = true)
{
    public static UiSettings Default { get; } = new(FontSettings.Default, new VariantSelection(), null);
}

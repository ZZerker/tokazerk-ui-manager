using System.Text.Json.Serialization;

namespace TokaZerkUIConfig.Domain;

public sealed record UiSettings
{
    [JsonConstructor]
    public UiSettings(FontSettings fonts, VariantSelection variants, bool checkForUpdatesOnStart = true)
    {
        this.Fonts = fonts;
        this.Variants = variants;
        this.CheckForUpdatesOnStart = checkForUpdatesOnStart;
    }

    public FontSettings Fonts { get; init; }

    public VariantSelection Variants { get; init; }

    public bool CheckForUpdatesOnStart { get; init; }

    public static UiSettings Default { get; } = new(FontSettings.Default, new VariantSelection());
}

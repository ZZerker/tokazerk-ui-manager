using System.Text.Json.Serialization;

namespace TokaZerkUIConfig.Domain;

public sealed record UiSettings
{
    [JsonConstructor]
    public UiSettings(FontSettings fonts, VariantSelection variants, bool checkForUpdatesOnStart = true, UpdateChannel updateChannel = UpdateChannel.Stable)
    {
        this.Fonts = fonts;
        this.Variants = variants;
        this.CheckForUpdatesOnStart = checkForUpdatesOnStart;
        this.UpdateChannel = updateChannel;
    }

    public FontSettings Fonts { get; init; }

    public VariantSelection Variants { get; init; }

    public bool CheckForUpdatesOnStart { get; init; }

    public UpdateChannel UpdateChannel { get; init; }

    public static UiSettings Default { get; } = new(FontSettings.Default, new VariantSelection());
}

namespace TokaZerkUIConfig.Domain;

public enum VariantKind
{
    MapSize,
    TargetWindow,
    FloatTarget,
}

public sealed record VariantChoice(string Id, string Label, string? SourcePath)
{
    public const string DEFAULT_ID = "default";
}

public sealed record Variant(VariantKind Kind, string TargetPath, IReadOnlyList<VariantChoice> Choices, IReadOnlyList<string> Preserve);

public static class VariantTable
{
    public static IReadOnlyList<Variant> All { get; } =
    [
        new Variant(
            VariantKind.MapSize,
            "Maps",
            [
                new VariantChoice(VariantChoice.DEFAULT_ID, "Default", null),
                new VariantChoice("large", "Large", "Maps_large"),
                new VariantChoice("small", "Small", "Maps_small"),
            ],
            ["Warmap", "regions.dat"]),
        new Variant(
            VariantKind.TargetWindow,
            "custom2_window.xml",
            [
                new VariantChoice(VariantChoice.DEFAULT_ID, "Blue (TokaZerk)", "Options/TargetWindow/blue(TokaZerk)/custom2_window.xml"),
                new VariantChoice("purple", "Purple", "Options/TargetWindow/purple/custom2_window.xml"),
            ],
            []),
        new Variant(
            VariantKind.FloatTarget,
            "float_target_window.xml",
            [
                new VariantChoice(VariantChoice.DEFAULT_ID, "Default", null),
                new VariantChoice("hud", "HUD", "Options/floatTargetWindow/float_target_window.xml"),
            ],
            []),
    ];

    public static Variant Get(VariantKind kind) => All.First(v => v.Kind == kind);
}

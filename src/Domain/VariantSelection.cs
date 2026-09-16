namespace TokaZerkUIConfig.Domain;

public sealed record VariantSelection(
    string MapSize = VariantChoice.DEFAULT_ID,
    string TargetWindow = VariantChoice.DEFAULT_ID,
    string FloatTarget = VariantChoice.DEFAULT_ID)
{
    public string Get(VariantKind kind) => kind switch
    {
        VariantKind.MapSize => this.MapSize,
        VariantKind.TargetWindow => this.TargetWindow,
        VariantKind.FloatTarget => this.FloatTarget,
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    public VariantSelection With(VariantKind kind, string choiceId) => kind switch
    {
        VariantKind.MapSize => this with { MapSize = choiceId },
        VariantKind.TargetWindow => this with { TargetWindow = choiceId },
        VariantKind.FloatTarget => this with { FloatTarget = choiceId },
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}

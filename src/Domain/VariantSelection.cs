namespace TokaZerkUIConfig.Domain;

public sealed record VariantSelection(
    string MapSize = VariantChoice.DefaultId,
    string TargetWindow = VariantChoice.DefaultId,
    string FloatTarget = VariantChoice.DefaultId)
{
    public string Get(VariantKind kind) => kind switch
    {
        VariantKind.MapSize => MapSize,
        VariantKind.TargetWindow => TargetWindow,
        VariantKind.FloatTarget => FloatTarget,
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

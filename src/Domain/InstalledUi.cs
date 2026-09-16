namespace TokaZerkUIConfig.Domain;

public enum InstalledUiKind
{
    TokaZerk,
    Other,
    None
}

public sealed record InstalledUi(InstalledUiKind Kind, SemVer? Version);

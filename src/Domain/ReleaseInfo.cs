namespace TokaZerkUIConfig.Domain;

public sealed record ReleaseInfo(SemVer Version, string Tag, string AssetName, string AssetUrl, string Notes);

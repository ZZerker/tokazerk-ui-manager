namespace TokaZerkUIConfig.Domain;

public static class ReleaseRepositories
{
    public const string UI_OWNER = "tokajer";
    public const string UI_REPO = "tokajerui";
    public const string TOOL_OWNER = "tokajer";
    public const string TOOL_REPO = "TokaZerkUIConfig";

    public static bool IsUiAsset(string name) =>
        name.StartsWith("TokaZerkUI-v", StringComparison.Ordinal) && name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);

    public static bool IsToolAsset(string name, string rid) =>
        name.Contains($"-{rid}.", StringComparison.OrdinalIgnoreCase) || name.EndsWith($"-{rid}", StringComparison.OrdinalIgnoreCase);
}

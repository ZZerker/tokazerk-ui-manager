namespace TokaZerkUIConfig.Domain;

public static class ReleaseRepositories
{
    public const string UI_OWNER = "tokajer";
    public const string UI_REPO = "tokajerui";
    public const string TOOL_OWNER = "tokajer";
    public const string TOOL_REPO = "tokazerk-ui-manager";

    public static bool IsUiAsset(string name) =>
        name.StartsWith("TokaZerkUI-v", StringComparison.Ordinal) && name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);

    public static bool IsToolAsset(string name, string rid) => rid switch
    {
        "win-x64" => name.Equals("TokaZerkUIManager-win-x64.exe", StringComparison.OrdinalIgnoreCase),
        "linux-x64" => name.Equals("TokaZerkUIManager-linux-x64", StringComparison.OrdinalIgnoreCase),
        _ => false,
    };
}

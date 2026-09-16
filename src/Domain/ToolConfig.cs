namespace TokaZerkUIConfig.Domain;

public sealed record ToolConfig(
    UpdateChannel UpdateChannel = UpdateChannel.Stable,
    bool CheckForUpdatesOnStart = true)
{
    public static ToolConfig Default { get; } = new();
}

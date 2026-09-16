namespace TokaZerkUIConfig.Domain;

public sealed record PreviewWindow(string Label, string WindowId);

public static class PreviewWindows
{
    public static IReadOnlyList<PreviewWindow> All { get; } =
    [
        new PreviewWindow("Main", "new_summary_window"),
        new PreviewWindow("Group", "stats_group"),
        new PreviewWindow("Group v2", "new_group_window"),
        new PreviewWindow("Target", "custom2_window"),
        new PreviewWindow("Chat", "chat"),
        new PreviewWindow("Mini stats", "custom6_window"),
        new PreviewWindow("Mini resists", "custom15_window"),
    ];
}

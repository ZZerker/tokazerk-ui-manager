namespace TokaZerkUIConfig.Domain;

public sealed record PreviewWindow(string Label, string WindowId);

public static class PreviewWindows
{
    public static IReadOnlyList<PreviewWindow> All { get; } =
    [
        new PreviewWindow("Main", "summary"),
        new PreviewWindow("Main (new)", "new_summary_window"),
        new PreviewWindow("Group", "new_group_window"),
        new PreviewWindow("Target", "custom2_window"),
        new PreviewWindow("Chat", "chat"),
        new PreviewWindow("Mini stats", "custom6_window"),
        new PreviewWindow("Mini resists", "custom15_window"),
        new PreviewWindow("Attributes", "stats_attributes"),
        new PreviewWindow("Quest journal", "new_quest_journal"),
    ];
}

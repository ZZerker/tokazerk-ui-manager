namespace TokaZerkUIConfig.Application;

public sealed record UpdateUiResult(ApplyResult Install, IReadOnlyList<string> Reapplied, IReadOnlyList<string> Failed);

namespace TokaZerkUIConfig.Application;

public sealed record ApplyResult(bool Success, IReadOnlyList<string> Warnings, string? Error)
{
    public static ApplyResult Ok(IReadOnlyList<string>? warnings = null) => new(true, warnings ?? [], null);

    public static ApplyResult Fail(string error) => new(false, [], error);
}

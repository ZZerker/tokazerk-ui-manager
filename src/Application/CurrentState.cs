using TokaZerkUIConfig.Domain;

namespace TokaZerkUIConfig.Application;

public sealed record CurrentState(
    UiSettings Settings,
    FontSettings? FontsInXml,
    InstalledUi InstalledUi,
    string? SettingsError,
    string? FontError,
    string? IdentityError);

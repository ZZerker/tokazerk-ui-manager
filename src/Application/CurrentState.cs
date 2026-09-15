using TokaZerkUIConfig.Domain;

namespace TokaZerkUIConfig.Application;

public sealed record CurrentState(UiSettings Settings, FontSettings? FontsInXml, SemVer? UiVersion, string? Error);

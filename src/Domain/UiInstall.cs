namespace TokaZerkUIConfig.Domain;

public enum InstallSource
{
    EdenLauncher,
    BlackthornLauncher,
    PrefixScan,
    WellKnown,
    Manual,
}

public enum ServerKind
{
    Eden,
    Blackthorn,
    Unknown,
}

public sealed record UiInstall(string GameRoot, string CustomPath, InstallSource Source, ServerKind Server);

using TokaZerkUIConfig.Domain;

namespace TokaZerkUIConfig.Application;

public sealed record UpdateCheck(ReleaseInfo? UiRelease, SemVer? InstalledUiVersion, ReleaseInfo? ToolRelease, SemVer ToolVersion)
{
    public bool UiUpdateAvailable => this.UiRelease is not null && (this.InstalledUiVersion is null || this.UiRelease.Version > this.InstalledUiVersion.Value);

    public bool ToolUpdateAvailable => this.ToolRelease is not null && this.ToolRelease.Version > this.ToolVersion;
}

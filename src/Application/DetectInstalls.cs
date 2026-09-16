using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Domain.Ports;

namespace TokaZerkUIConfig.Application;

public sealed class DetectInstalls(IInstallLocator installLocator)
{
    public Task<IReadOnlyList<UiInstall>> ExecuteAsync(CancellationToken ct) => installLocator.DetectAsync(ct);
}

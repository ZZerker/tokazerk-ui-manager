using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Domain.Ports;

namespace TokaZerkUIConfig.Application;

public sealed class DetectInstalls
{
    private readonly IInstallLocator _installLocator;

    public DetectInstalls(IInstallLocator installLocator)
    {
        _installLocator = installLocator;
    }

    public Task<IReadOnlyList<UiInstall>> ExecuteAsync(CancellationToken ct) => _installLocator.DetectAsync(ct);
}

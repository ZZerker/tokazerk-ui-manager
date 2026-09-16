using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Domain.Ports;

namespace TokaZerkUIConfig.Application;

public sealed class ValidateInstall(IInstallLocator installLocator)
{
    public UiInstall? Execute(string path) => installLocator.Validate(path);
}

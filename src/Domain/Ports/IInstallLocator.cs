namespace TokaZerkUIConfig.Domain.Ports;

public interface IInstallLocator
{
    Task<IReadOnlyList<UiInstall>> DetectAsync(CancellationToken ct);

    UiInstall? Validate(string path);
}

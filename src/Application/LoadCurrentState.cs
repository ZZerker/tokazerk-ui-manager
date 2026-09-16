using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Domain.Ports;

namespace TokaZerkUIConfig.Application;

public sealed class LoadCurrentState(
    ISettingsRepository settingsRepository,
    IFontDefinitionStore fontDefinitionStore,
    IInstalledUiReader installedUiReader)
{
    public async Task<CurrentState> ExecuteAsync(string customPath, CancellationToken ct)
    {
        InstalledUi installedUi;
        try
        {
            installedUi = await installedUiReader.ReadAsync(customPath, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new CurrentState(
                UiSettings.Default,
                null,
                new InstalledUi(InstalledUiKind.None, null),
                null,
                null,
                ex.Message);
        }

        if (installedUi.Kind != InstalledUiKind.TokaZerk)
        {
            return new CurrentState(UiSettings.Default, null, installedUi, null, null, null);
        }

        UiSettings settings;
        string? settingsError = null;
        try
        {
            settings = await settingsRepository.LoadAsync(customPath, ct).ConfigureAwait(false) ?? UiSettings.Default;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            settings = UiSettings.Default;
            settingsError = ex.Message;
        }

        FontSettings? fontsInXml = null;
        string? fontError = null;
        try
        {
            fontsInXml = await fontDefinitionStore.ReadAsync(customPath, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FontDefinitionsNotFoundException)
        {
            fontError = ex.Message;
        }

        return new CurrentState(settings, fontsInXml, installedUi, settingsError, fontError, null);
    }
}

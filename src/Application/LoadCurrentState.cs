using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Domain.Ports;

namespace TokaZerkUIConfig.Application;

public sealed class LoadCurrentState(ISettingsRepository settingsRepository, IFontDefinitionStore fontDefinitionStore, IUiVersionReader uiVersionReader)
{
    public async Task<CurrentState> ExecuteAsync(string customPath, CancellationToken ct)
    {
        try
        {
            var settings = await settingsRepository.LoadAsync(customPath, ct).ConfigureAwait(false) ?? UiSettings.Default;
            var fontsInXml = await fontDefinitionStore.ReadAsync(customPath, ct).ConfigureAwait(false);
            var version = settings.InstalledUiVersion ?? await uiVersionReader.ReadAsync(customPath, ct).ConfigureAwait(false);

            return new CurrentState(settings, fontsInXml, version, null);
        }
        catch (FontDefinitionsNotFoundException ex)
        {
            return new CurrentState(UiSettings.Default, null, null, ex.Message);
        }
        catch (IOException ex)
        {
            return new CurrentState(UiSettings.Default, null, null, ex.Message);
        }
    }
}

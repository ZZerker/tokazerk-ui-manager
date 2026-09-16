using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Domain.Ports;

namespace TokaZerkUIConfig.Application;

public sealed class LoadCurrentState(ISettingsRepository settingsRepository, IFontDefinitionStore fontDefinitionStore, IUiVersionReader uiVersionReader)
{
    public async Task<CurrentState> ExecuteAsync(string customPath, CancellationToken ct)
    {
        UiSettings settings;
        try
        {
            settings = await settingsRepository.LoadAsync(customPath, ct).ConfigureAwait(false) ?? UiSettings.Default;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new CurrentState(UiSettings.Default, null, null, ex.Message);
        }

        FontSettings? fontsInXml = null;
        string? error = null;
        try
        {
            fontsInXml = await fontDefinitionStore.ReadAsync(customPath, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FontDefinitionsNotFoundException)
        {
            error = ex.Message;
        }

        var version = settings.InstalledUiVersion;
        if (version is null)
        {
            try
            {
                version = await uiVersionReader.ReadAsync(customPath, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FontDefinitionsNotFoundException)
            {
                error = error is null ? ex.Message : string.Join(Environment.NewLine, error, ex.Message);
            }
        }

        return new CurrentState(settings, fontsInXml, version, error);
    }
}

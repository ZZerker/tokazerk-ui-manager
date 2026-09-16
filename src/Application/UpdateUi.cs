using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Domain.Ports;

namespace TokaZerkUIConfig.Application;

public sealed class UpdateUi(InstallUi installUi, IFontDefinitionStore fontDefinitionStore, IVariantStore variantStore, ISettingsRepository settingsRepository, IBackupStore backupStore)
{
    public async Task<UpdateUiResult> ExecuteAsync(string customPath, ReleaseInfo release, IProgress<double>? progress, CancellationToken ct)
    {
        var install = await installUi.ExecuteAsync(customPath, release, progress, ct).ConfigureAwait(false);
        if (!install.Success)
        {
            return new UpdateUiResult(install, [], []);
        }

        var settings = await settingsRepository.LoadAsync(customPath, ct).ConfigureAwait(false) ?? UiSettings.Default;

        // Delete the pre-update backup so map "default" restores the new release's Maps/, not the stale copy.
        await backupStore.ClearAsync(customPath, ct).ConfigureAwait(false);

        var reapplied = new List<string>();
        var failed = new List<string>();

        if (settings.Fonts != FontSettings.Default)
        {
            try
            {
                await fontDefinitionStore.WriteAsync(customPath, settings.Fonts, ct).ConfigureAwait(false);
                reapplied.Add("Fonts");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failed.Add($"Fonts: {ex.Message}");
            }
        }

        foreach (var variant in VariantTable.All)
        {
            var choiceId = settings.Variants.Get(variant.Kind);
            if (choiceId == VariantChoice.DEFAULT_ID)
            {
                continue;
            }

            var choice = variant.Choices.FirstOrDefault(c => c.Id == choiceId);
            if (choice is null)
            {
                continue;
            }

            try
            {
                await variantStore.ApplyAsync(customPath, variant, choice, ct).ConfigureAwait(false);
                reapplied.Add(variant.Kind.ToString());
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failed.Add($"{variant.Kind}: {ex.Message}");
            }
        }

        return new UpdateUiResult(install, reapplied, failed);
    }
}

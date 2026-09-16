using DaocUiForge.Core.Ingest;
using DaocUiForge.Core.Model;
using DaocUiForge.Core.Render;
using SkiaSharp;
using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Domain.Ports;

namespace TokaZerkUIConfig.Infrastructure;

public sealed class ForgePreviewRenderer : IUiPreviewRenderer, IDisposable
{
    private static readonly IReadOnlyList<string> SkippedFolders =
        ["Maps", "Maps_large", "Maps_small", "Options", "warmap", "tokazerk_config"];

    // Windows paths are case-insensitive, Linux paths are not.
    private static readonly StringComparison PathComparison =
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private readonly SemaphoreSlim gate = new(1, 1);

    private string? cachedPath;
    private RenderContext? context;

    public async Task<PreviewImage> RenderAsync(string customPath, string windowId, FontSettings settings, CancellationToken ct)
    {
        var normalizedPath = Path.GetFullPath(customPath);

        await this.gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (this.context is null || !string.Equals(this.cachedPath, normalizedPath, PathComparison))
            {
                this.context?.Dispose();
                this.context = null;
                this.cachedPath = null;

                // PackageLoader reads directly from System.IO, not IFileSystem: it belongs to the
                // external DaocUiForge.Core dependency and is not something this repo owns.
                var package = await Task.Run(() => PackageLoader.LoadFromDirectory(normalizedPath, SkippedFolders), ct).ConfigureAwait(false);
                this.context = new RenderContext(package);
                this.cachedPath = normalizedPath;
                ct.ThrowIfCancellationRequested();
            }

            var context = this.context;
            ApplyFontSettings(context.Package, settings);

            var window = context.Package.Windows.Find(w => string.Equals(w.Id, windowId, StringComparison.OrdinalIgnoreCase));
            if (window is null)
            {
                throw new PreviewWindowNotFoundException(windowId);
            }

            return await Task.Run(
                () =>
                {
                    using var rendered = WindowRenderer.Render(context, window);
                    using var image = SKImage.FromBitmap(rendered.Bitmap);
                    using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                    return new PreviewImage(data.ToArray(), rendered.Bitmap.Width, rendered.Bitmap.Height, rendered.Failures.Count);
                },
                ct).ConfigureAwait(false);
        }
        finally
        {
            this.gate.Release();
        }
    }

    public void Invalidate()
    {
        this.gate.Wait();
        try
        {
            this.context?.Dispose();
            this.context = null;
            this.cachedPath = null;
        }
        finally
        {
            this.gate.Release();
        }
    }

    public void Dispose()
    {
        this.gate.Wait();
        try
        {
            this.context?.Dispose();
            this.context = null;
        }
        finally
        {
            this.gate.Release();
        }

        this.gate.Dispose();
    }

    private static void ApplyFontSettings(Package package, FontSettings settings)
    {
        foreach (var tier in Enum.GetValues<FontTier>())
        {
            foreach (var name in FontTierInfo.DefinitionNames(tier))
            {
                if (package.Fonts.TryGetValue(name, out var font))
                {
                    font.Height = settings.Get(tier);
                }
            }
        }
    }
}

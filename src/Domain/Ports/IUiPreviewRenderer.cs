namespace TokaZerkUIConfig.Domain.Ports;

public interface IUiPreviewRenderer
{
    Task<PreviewImage> RenderAsync(string customPath, string windowId, FontSettings settings, CancellationToken ct);

    // Drops the cached package so the next RenderAsync reloads from disk; the Application layer calls it after Apply/UpdateUi.
    void Invalidate();
}

public sealed class PreviewWindowNotFoundException(string windowId): Exception($"Preview window not found: {windowId}")
{
    public string WindowId { get; } = windowId;
}

namespace TokaZerkUIConfig.Domain;

public sealed record PreviewImage(byte[] PngBytes, int Width, int Height, int FailureCount);

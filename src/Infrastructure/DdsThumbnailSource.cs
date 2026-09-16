using System.Buffers.Binary;
using System.IO.Abstractions;
using DaocUiForge.Core.Formats;
using SkiaSharp;
using TokaZerkUIConfig.Domain.Ports;

namespace TokaZerkUIConfig.Infrastructure;

public sealed class DdsThumbnailSource(IFileSystem fileSystem) : IMapThumbnailSource
{
    private const int DDS_HEADER_LENGTH = 128;
    private const uint DDS_MAGIC = 0x20534444;
    private const int HEIGHT_OFFSET = 12;
    private const int WIDTH_OFFSET = 16;
    private const int MAX_DIMENSION = 8192;
    private const long MAX_PIXEL_COUNT = 16_777_216;
    private const long MAX_FILE_SIZE_BYTES = 64L * 1024 * 1024;

    public Task<byte[]> LoadPngAsync(string ddsPath, CancellationToken ct) =>
        Task.Run(() => this.LoadPng(ddsPath, ct), ct);

    private byte[] LoadPng(string ddsPath, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (!fileSystem.File.Exists(ddsPath))
        {
            throw new FileNotFoundException($"Map image not found: {ddsPath}", ddsPath);
        }

        if (fileSystem.FileInfo.New(ddsPath).Length > MAX_FILE_SIZE_BYTES)
        {
            throw new InvalidDataException($"Map image is too large: {ddsPath}");
        }

        byte[] ddsBytes;
        try
        {
            ddsBytes = fileSystem.File.ReadAllBytes(ddsPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new IOException($"Could not read map image: {ddsPath}", ex);
        }

        ct.ThrowIfCancellationRequested();
        ValidateDds(ddsBytes, ddsPath);

        try
        {
            using var bitmap = DdsDecoder.Decode(ddsBytes);
            if (bitmap is null)
            {
                throw new InvalidDataException($"Map image is not a supported DDS file: {ddsPath}");
            }

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not InvalidDataException)
        {
            throw new InvalidDataException($"Could not decode map image: {ddsPath}", ex);
        }
    }

    private static void ValidateDds(byte[] ddsBytes, string ddsPath)
    {
        if (ddsBytes.Length < DDS_HEADER_LENGTH
            || ddsBytes.LongLength > MAX_FILE_SIZE_BYTES
            || BinaryPrimitives.ReadUInt32LittleEndian(ddsBytes) != DDS_MAGIC)
        {
            throw new InvalidDataException($"Map image is not a supported DDS file: {ddsPath}");
        }

        var height = BinaryPrimitives.ReadUInt32LittleEndian(ddsBytes.AsSpan(HEIGHT_OFFSET));
        var width = BinaryPrimitives.ReadUInt32LittleEndian(ddsBytes.AsSpan(WIDTH_OFFSET));
        if (width == 0 || height == 0 || width > MAX_DIMENSION || height > MAX_DIMENSION)
        {
            throw new InvalidDataException($"Map image has invalid dimensions: {ddsPath}");
        }

        long pixelCount;
        try
        {
            pixelCount = checked((long)width * height);
        }
        catch (OverflowException ex)
        {
            throw new InvalidDataException($"Map image dimensions are too large: {ddsPath}", ex);
        }

        if (pixelCount > MAX_PIXEL_COUNT)
        {
            throw new InvalidDataException($"Map image dimensions are too large: {ddsPath}");
        }
    }
}

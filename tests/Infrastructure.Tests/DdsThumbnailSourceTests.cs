using System.IO.Abstractions.TestingHelpers;
using TokaZerkUIConfig.Infrastructure;
using Xunit;

namespace TokaZerkUIConfig.Infrastructure.Tests;

public class DdsThumbnailSourceTests
{
    [Fact]
    public async Task LoadPngAsyncValidUncompressedDdsReturnsPng()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("custom/Maps/r001.dds", new MockFileData(CreateUncompressedDds()));
        var source = new DdsThumbnailSource(fileSystem);

        var png = await source.LoadPngAsync("custom/Maps/r001.dds", CancellationToken.None);

        byte[] pngSignature = [137, 80, 78, 71, 13, 10, 26, 10];
        Assert.True(png.AsSpan(0, pngSignature.Length).SequenceEqual(pngSignature));
    }

    [Fact]
    public async Task LoadPngAsyncInvalidDdsReturnsUserFacingFailure()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("custom/Maps/r001.dds", new MockFileData([1, 2, 3]));
        var source = new DdsThumbnailSource(fileSystem);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => source.LoadPngAsync("custom/Maps/r001.dds", CancellationToken.None));

        Assert.Contains("not a supported DDS file", exception.Message);
    }

    [Fact]
    public async Task LoadPngAsyncOversizedDimensionsRejectsBeforeDecode()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("custom/Maps/r001.dds", new MockFileData(CreateUncompressedDds(8192, 8192)));
        var source = new DdsThumbnailSource(fileSystem);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => source.LoadPngAsync("custom/Maps/r001.dds", CancellationToken.None));

        Assert.Contains("dimensions are too large", exception.Message);
    }

    [Fact]
    public async Task LoadPngAsyncMissingDdsReturnsUserFacingFailure()
    {
        var source = new DdsThumbnailSource(new MockFileSystem());

        var exception = await Assert.ThrowsAsync<FileNotFoundException>(
            () => source.LoadPngAsync("custom/Maps/r001.dds", CancellationToken.None));

        Assert.Contains("Map image not found", exception.Message);
    }

    private static byte[] CreateUncompressedDds(uint width = 1, uint height = 1)
    {
        var bytes = new byte[132];
        WriteUInt32(bytes, 0, 0x20534444);
        WriteUInt32(bytes, 12, height);
        WriteUInt32(bytes, 16, width);
        WriteUInt32(bytes, 88, 32);
        bytes[128] = 10;
        bytes[129] = 20;
        bytes[130] = 30;
        bytes[131] = 255;
        return bytes;
    }

    private static void WriteUInt32(byte[] destination, int offset, uint value)
    {
        var bytes = BitConverter.GetBytes(value);
        Array.Copy(bytes, 0, destination, offset, bytes.Length);
    }
}

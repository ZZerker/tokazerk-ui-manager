using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.IO.Compression;
using TokaZerkUIConfig.Infrastructure;
using Xunit;

namespace TokaZerkUIConfig.Infrastructure.Tests;

public class ZipUiArchiveStoreTests
{
    [Fact]
    public async Task ArchiveAsyncCreatesCollisionSafeArchiveContainingEveryFileAtRoot()
    {
        var tempRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"TokaZerkUIConfig-{Guid.NewGuid():N}"));
        try
        {
            var customPath = Path.Combine(tempRoot, "ui", "custom");
            var nestedPath = Path.Combine(customPath, "Options", "TargetWindow");
            Directory.CreateDirectory(nestedPath);
            await File.WriteAllTextAsync(
                Path.Combine(customPath, "tokazerk.json"),
                """{"name":"TokaZerkUI","version":"1.0.12","built":"2026-09-15"}""");
            await File.WriteAllTextAsync(Path.Combine(customPath, "assets.xml"), "assets");
            await File.WriteAllTextAsync(Path.Combine(nestedPath, "custom2_window.xml"), "window");

            var fileSystem = new FileSystem();
            var reader = new TokazerkJsonReader(fileSystem);
            var store = new ZipUiArchiveStore(fileSystem, reader);
            var occupiedArchivePath = Path.Combine(tempRoot, "ui", "backups", "custom-tokazerk-1.0.12.zip");
            Directory.CreateDirectory(occupiedArchivePath);

            var firstArchivePath = await store.ArchiveAsync(customPath, CancellationToken.None);
            var secondArchivePath = await store.ArchiveAsync(customPath, CancellationToken.None);

            Assert.NotEqual(firstArchivePath, secondArchivePath);
            Assert.True(File.Exists(firstArchivePath));
            Assert.True(File.Exists(secondArchivePath));
            Assert.True(Directory.Exists(occupiedArchivePath));
            Assert.Equal("custom-tokazerk-1.0.12-2.zip", Path.GetFileName(firstArchivePath));
            Assert.Equal("custom-tokazerk-1.0.12-3.zip", Path.GetFileName(secondArchivePath));

            var expectedEntries = Directory.GetFiles(customPath, "*", SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(customPath, path).Replace('\\', '/'))
                .Order()
                .ToArray();
            using var archive = ZipFile.OpenRead(firstArchivePath);
            var actualEntries = archive.Entries
                .Where(entry => !string.IsNullOrEmpty(entry.Name))
                .Select(entry => entry.FullName)
                .Order()
                .ToArray();

            Assert.Equal(expectedEntries, actualEntries);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ArchiveAsyncRejectsReparsePointBeforeCreatingArchive()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(
            "custom/tokazerk.json",
            new MockFileData(
                """{"name":"TokaZerkUI","version":"1.0.12","built":"2026-09-15"}"""));
        fileSystem.AddFile(
            "custom/link",
            new MockFileData("target")
            {
                Attributes = FileAttributes.ReparsePoint,
            });
        var reader = new TokazerkJsonReader(fileSystem);
        var store = new ZipUiArchiveStore(fileSystem, reader);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => store.ArchiveAsync("custom", CancellationToken.None));

        Assert.Contains("reparse point", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void VerifyRejectsZipWhoseEntriesAreNotUnderCustomRoot()
    {
        var tempRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"TokaZerkUIConfig-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(tempRoot);
        try
        {
            var zipPath = Path.Combine(tempRoot, "release.zip");
            using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                archive.CreateEntry("tokazerk.json").Open().Dispose();
            }

            var fileSystem = new FileSystem();
            var store = new ZipUiArchiveStore(fileSystem, new TokazerkJsonReader(fileSystem));
            var customPath = Path.Combine(tempRoot, "custom");

            var exception = Assert.Throws<InvalidDataException>(() => store.Verify(zipPath, customPath));
            Assert.Contains("custom/", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void VerifyRejectsZipMissingTokazerkJson()
    {
        var tempRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"TokaZerkUIConfig-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(tempRoot);
        try
        {
            var zipPath = Path.Combine(tempRoot, "release.zip");
            using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                archive.CreateEntry("custom/assets.xml").Open().Dispose();
            }

            var fileSystem = new FileSystem();
            var store = new ZipUiArchiveStore(fileSystem, new TokazerkJsonReader(fileSystem));
            var customPath = Path.Combine(tempRoot, "custom");

            var exception = Assert.Throws<InvalidDataException>(() => store.Verify(zipPath, customPath));
            Assert.Contains("tokazerk.json", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task InstallAsyncClearsStrayFileAndExtractsTreeWithoutCustomPrefix()
    {
        var tempRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"TokaZerkUIConfig-{Guid.NewGuid():N}"));
        var customPath = Path.Combine(tempRoot, "custom");
        Directory.CreateDirectory(customPath);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(customPath, "stray.txt"), "old content");

            var zipPath = Path.Combine(tempRoot, "release.zip");
            using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                using (var entryStream = archive.CreateEntry("custom/tokazerk.json").Open())
                using (var writer = new StreamWriter(entryStream))
                {
                    await writer.WriteAsync("""{"name":"TokaZerkUI","version":"1.0.13","built":"2026-09-16"}""");
                }

                using (var entryStream = archive.CreateEntry("custom/Options/assets.xml").Open())
                using (var writer = new StreamWriter(entryStream))
                {
                    await writer.WriteAsync("assets");
                }
            }

            var fileSystem = new FileSystem();
            var store = new ZipUiArchiveStore(fileSystem, new TokazerkJsonReader(fileSystem));

            await store.InstallAsync(zipPath, customPath, CancellationToken.None);

            Assert.False(File.Exists(Path.Combine(customPath, "stray.txt")));
            Assert.True(File.Exists(Path.Combine(customPath, "tokazerk.json")));
            Assert.True(File.Exists(Path.Combine(customPath, "Options", "assets.xml")));
            Assert.False(File.Exists(zipPath));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task InstallAsyncRejectsZipSlipEntryBeforeDeletingAnything()
    {
        var tempRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"TokaZerkUIConfig-{Guid.NewGuid():N}"));
        var customPath = Path.Combine(tempRoot, "custom");
        Directory.CreateDirectory(customPath);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(customPath, "stray.txt"), "old content");

            var zipPath = Path.Combine(tempRoot, "release.zip");
            using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                using (var entryStream = archive.CreateEntry("custom/tokazerk.json").Open())
                using (var writer = new StreamWriter(entryStream))
                {
                    await writer.WriteAsync("""{"name":"TokaZerkUI","version":"1.0.13","built":"2026-09-16"}""");
                }

                using (var entryStream = archive.CreateEntry("custom/../evil.txt").Open())
                using (var writer = new StreamWriter(entryStream))
                {
                    await writer.WriteAsync("evil");
                }
            }

            var fileSystem = new FileSystem();
            var store = new ZipUiArchiveStore(fileSystem, new TokazerkJsonReader(fileSystem));

            await Assert.ThrowsAsync<InvalidDataException>(
                () => store.InstallAsync(zipPath, customPath, CancellationToken.None));

            Assert.False(File.Exists(Path.Combine(tempRoot, "evil.txt")));
            Assert.True(File.Exists(Path.Combine(customPath, "stray.txt")));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}

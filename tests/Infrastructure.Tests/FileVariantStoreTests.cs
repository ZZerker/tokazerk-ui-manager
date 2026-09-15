using System.IO.Abstractions.TestingHelpers;
using System.Text;
using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Infrastructure;
using Xunit;

namespace TokaZerkUIConfig.Infrastructure.Tests;

public class FileVariantStoreTests
{
    [Fact]
    public async Task MapSizeSwitchToLarge_KeepsWarmapAndRegions_ReplacesOtherFiles()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("custom/Maps/Warmap/x.tga", new MockFileData("original-warmap"));
        fileSystem.AddFile("custom/Maps/regions.dat", new MockFileData("original-regions"));
        fileSystem.AddFile("custom/Maps/r001.dds", new MockFileData("original-r001"));
        fileSystem.AddFile("custom/Maps_large/Warmap/x.tga", new MockFileData("large-warmap"));
        fileSystem.AddFile("custom/Maps_large/regions.dat", new MockFileData("large-regions"));
        fileSystem.AddFile("custom/Maps_large/r001.dds", new MockFileData("large-r001"));

        var backupStore = new FolderBackupStore(fileSystem);
        var store = new FileVariantStore(fileSystem, backupStore);
        var variant = VariantTable.Get(VariantKind.MapSize);
        var choice = variant.Choices.Single(c => c.Id == "large");

        await store.ApplyAsync("custom", variant, choice, CancellationToken.None);

        Assert.Equal("original-warmap", ReadText(fileSystem, "custom/Maps/Warmap/x.tga"));
        Assert.Equal("original-regions", ReadText(fileSystem, "custom/Maps/regions.dat"));
        Assert.Equal("large-r001", ReadText(fileSystem, "custom/Maps/r001.dds"));
    }

    [Fact]
    public async Task TargetWindowSwitch_BacksUpOnce_AndDefaultRestoresOriginal()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("custom/custom2_window.xml", new MockFileData("original-blue"));
        fileSystem.AddFile("custom/Options/TargetWindow/purple/custom2_window.xml", new MockFileData("purple"));
        fileSystem.AddFile("custom/Options/TargetWindow/blue(TokaZerk)/custom2_window.xml", new MockFileData("blue-source"));

        var backupStore = new FolderBackupStore(fileSystem);
        var store = new FileVariantStore(fileSystem, backupStore);
        var variant = VariantTable.Get(VariantKind.TargetWindow);
        var purple = variant.Choices.Single(c => c.Id == "purple");
        var blue = variant.Choices.Single(c => c.Id == "default");

        await store.ApplyAsync("custom", variant, purple, CancellationToken.None);
        Assert.Equal("purple", ReadText(fileSystem, "custom/custom2_window.xml"));
        Assert.Equal("original-blue", ReadText(fileSystem, "custom/tokazerk_config/backup/custom2_window.xml"));

        // Second apply must not overwrite the backup with the now-purple content.
        await store.ApplyAsync("custom", variant, purple, CancellationToken.None);
        Assert.Equal("original-blue", ReadText(fileSystem, "custom/tokazerk_config/backup/custom2_window.xml"));

        await store.ApplyAsync("custom", variant, blue, CancellationToken.None);
        Assert.Equal("blue-source", ReadText(fileSystem, "custom/custom2_window.xml"));
    }

    [Fact]
    public async Task MapSizeSwitchToDefault_PreservesWarmapAndRegions_RestoresOtherFiles()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("custom/Maps/Warmap/x.tga", new MockFileData("original-warmap"));
        fileSystem.AddFile("custom/Maps/regions.dat", new MockFileData("original-regions"));
        fileSystem.AddFile("custom/Maps/r001.dds", new MockFileData("original-r001"));
        fileSystem.AddFile("custom/Maps_large/Warmap/x.tga", new MockFileData("large-warmap"));
        fileSystem.AddFile("custom/Maps_large/regions.dat", new MockFileData("large-regions"));
        fileSystem.AddFile("custom/Maps_large/r001.dds", new MockFileData("large-r001"));

        var backupStore = new FolderBackupStore(fileSystem);
        var store = new FileVariantStore(fileSystem, backupStore);
        var variant = VariantTable.Get(VariantKind.MapSize);
        var large = variant.Choices.Single(c => c.Id == "large");
        var defaultChoice = variant.Choices.Single(c => c.Id == "default");

        await store.ApplyAsync("custom", variant, large, CancellationToken.None);
        fileSystem.AddFile("custom/Maps/regions.dat", new MockFileData("overwritten-regions"));

        await store.ApplyAsync("custom", variant, defaultChoice, CancellationToken.None);

        Assert.Equal("overwritten-regions", ReadText(fileSystem, "custom/Maps/regions.dat"));
        Assert.Equal("original-r001", ReadText(fileSystem, "custom/Maps/r001.dds"));
    }

    private static string ReadText(MockFileSystem fileSystem, string path) =>
        Encoding.UTF8.GetString(fileSystem.GetFile(path).Contents);
}

using System.IO.Abstractions.TestingHelpers;
using TokaZerkUIConfig.Infrastructure;
using Xunit;

namespace TokaZerkUIConfig.Infrastructure.Tests;

// ApplyAsync swaps Environment.ProcessPath and starts a new process, which is the running
// test host itself; there is no seam to fake that, so only CleanupOnStart is covered here.
public class ExeSwapSelfUpdaterTests
{
    [Fact]
    public void CleanupOnStartDeletesLeftoverOldFileWhenPresent()
    {
        var processPath = Environment.ProcessPath;
        Assert.NotNull(processPath);

        var oldPath = processPath + ".old";
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(oldPath, new MockFileData("stale"));
        var updater = new ExeSwapSelfUpdater(fileSystem);

        updater.CleanupOnStart();

        Assert.False(fileSystem.File.Exists(oldPath));
    }

    [Fact]
    public void CleanupOnStartDoesNothingWhenNoOldFileExists()
    {
        var fileSystem = new MockFileSystem();
        var updater = new ExeSwapSelfUpdater(fileSystem);

        var exception = Record.Exception(() => updater.CleanupOnStart());

        Assert.Null(exception);
    }
}

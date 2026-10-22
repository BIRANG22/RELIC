using RelicAutomation.Core;
using Xunit;

namespace RelicAutomation.Core.Tests;

public sealed class ProfileFileLoaderTests
{
    [Fact]
    public void Load_ResolvesResourceDirectoryRelativeToProfileFile()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var profilePath = Path.Combine(root, "profile.json");
        File.WriteAllText(profilePath, """
        {
          "name": "RELIC sample",
          "entryNode": "Start",
          "targetNode": "Done",
          "repeatCount": 3,
          "expectedClientWidth": 1920,
          "expectedClientHeight": 1080,
          "stopWhenWindowSizeChanges": true,
          "resourceDirectory": "resource"
        }
        """);

        try
        {
            var profile = ProfileFileLoader.Load(profilePath);

            Assert.Equal("RELIC sample", profile.Name);
            Assert.Equal(Path.GetFullPath(Path.Combine(root, "resource")), profile.ResourceDirectory);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Load_RejectsMalformedJson()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, "{ broken");

        try
        {
            Assert.Throws<InvalidDataException>(() => ProfileFileLoader.Load(path));
        }
        finally
        {
            File.Delete(path);
        }
    }
}

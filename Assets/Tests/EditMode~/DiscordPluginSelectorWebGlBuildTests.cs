using System.IO;
using NUnit.Framework;

public sealed class DiscordPluginSelectorWebGlBuildTests
{
    private const string SelectorPath =
        "Packages/com.discord.partnersdk/Editor/DiscordPluginSelector.cs";

    [Test]
    public void WebGlBuild_DoesNotReconfigureDiscordNativePlugins()
    {
        string source = File.ReadAllText(SelectorPath);
        const string webGlGuard =
            "if (report.summary.platform == BuildTarget.WebGL) {\n            return;\n        }";

        Assert.That(source.Split(webGlGuard).Length - 1, Is.EqualTo(2));
    }
}

using System.IO;
using NUnit.Framework;

public sealed class WebGlBuildFootprintOptimizationTests
{
    private const string ProjectSettingsPath = "ProjectSettings/ProjectSettings.asset";

    [Test]
    public void WebGlMemoryBudget_IsConfiguredForFourGigabytes()
    {
        string settings = File.ReadAllText(ProjectSettingsPath);

        StringAssert.Contains("webGLInitialMemorySize: 512", settings);
        StringAssert.Contains("webGLMaximumMemorySize: 4096", settings);
    }

    [TestCase("Anime VFX URP")]
    [TestCase("Combat Flipbook VFX URP")]
    [TestCase("Flipbook VFX URP")]
    [TestCase("Pixel Craft VFX URP")]
    [TestCase("Stylized VFX URP")]
    [TestCase("Wind VFX URP")]
    public void VefectsDemoResources_AreNotAutoIncludedByResourcesFolder(string packageName)
    {
        string demoRoot = Path.Combine(
            "Assets/Project/Download/vfx/Vefects",
            packageName,
            "Demo");

        Assert.That(Directory.Exists(Path.Combine(demoRoot, "Resources")), Is.False);
        Assert.That(Directory.Exists(Path.Combine(demoRoot, "Resources_ExcludedFromPlayer")), Is.True);
    }
}

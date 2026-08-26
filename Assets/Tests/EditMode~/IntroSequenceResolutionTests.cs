using System.IO;
using NUnit.Framework;

public sealed class IntroSequenceResolutionTests
{
    private const string ControllerPath = "Assets/Project/Scripts/IntroSequenceController.cs";
    private const string BootstrapScenePath = "Assets/Project/Scenes/YDM/Bootstrap.unity";

    [Test]
    public void Controller_DoesNotOptIntroRootOutOfResolutionFitting()
    {
        string source = File.ReadAllText(ControllerPath);

        Assert.That(source, Does.Not.Contain("introRoot.AddComponent<ResolutionCanvasFitOptOut>()"));
        Assert.That(source, Does.Not.Contain("introRoot.GetComponent<ResolutionCanvasFitOptOut>()"));
    }

    [Test]
    public void BootstrapIntroCanvas_UsesScaleWithScreenSizeReferenceResolution()
    {
        string scene = File.ReadAllText(BootstrapScenePath);
        const string introCanvasScalerBlock =
            "  m_GameObject: {fileID: 263836670}\n" +
            "  m_Enabled: 1\n" +
            "  m_EditorHideFlags: 0\n" +
            "  m_Script: {fileID: 11500000, guid: 0cd44c1031e13a943bb63640046fad76, type: 3}\n" +
            "  m_Name: \n" +
            "  m_EditorClassIdentifier: \n" +
            "  m_UiScaleMode: 1\n";

        Assert.That(scene, Does.Contain(introCanvasScalerBlock));
        Assert.That(scene, Does.Contain("  m_ReferenceResolution: {x: 1920, y: 1080}"));
    }
}

using System;
using System.IO;
using NUnit.Framework;

public sealed class SampleSceneArrowGlowTests
{
    private const string ScenePath = "Assets/Project/Scenes/YDM/SampleScene.unity";
    private const string GlowMaterialPath = "Assets/Project/Shaders/ArrowSoftGlow.mat";
    private const string OutlineScriptGuid = "e19747de3f5aca642ab2be37e372fb86";
    private const string GlowMaterialGuid = "256ea37469d140d28cf9b154a6e8a6a5";
    private const string GlowShaderGuid = "01d81b650437453fb08a6230ca19046a";

    [TestCase("Left")]
    [TestCase("Right")]
    public void ArrowCore_DoesNotUseHardOutline(string objectName)
    {
        string scene = File.ReadAllText(ScenePath);
        string gameObjectBlock = FindBlock(scene, "--- !u!1 &", $"  m_Name: {objectName}\n");
        string gameObjectId = ReadObjectId(gameObjectBlock);

        Assert.That(FindComponentBlock(scene, gameObjectId, OutlineScriptGuid), Is.Null);
    }

    [TestCase("Glow_Left", -523f)]
    [TestCase("Glow_Right", 521f)]
    public void ArrowGlow_UsesSharedSoftGlowMaterialAndPaddedRect(string objectName, float expectedX)
    {
        string scene = File.ReadAllText(ScenePath);
        string gameObjectBlock = FindBlock(scene, "--- !u!1 &", $"  m_Name: {objectName}\n");
        string gameObjectId = ReadObjectId(gameObjectBlock);
        string imageBlock = FindComponentBlock(scene, gameObjectId, "fe87c0e1cc204ed48ad3b37840f39efc");
        string rectBlock = FindOwnedBlock(scene, gameObjectId, "--- !u!224 &");

        Assert.That(imageBlock, Does.Contain($"m_Material: {{fileID: 2100000, guid: {GlowMaterialGuid}, type: 2}}"));
        Assert.That(imageBlock, Does.Contain("m_RaycastTarget: 0"));
        Assert.That(rectBlock, Does.Contain($"m_AnchoredPosition: {{x: {expectedX}, y: 0}}"));
        Assert.That(rectBlock, Does.Contain("m_SizeDelta: {x: 28, y: 28}"));
    }

    [Test]
    public void ArrowGlowMaterial_UsesSoftGlowShaderWithTunableFalloff()
    {
        string material = File.ReadAllText(GlowMaterialPath);

        Assert.That(material, Does.Contain($"m_Shader: {{fileID: 4800000, guid: {GlowShaderGuid}, type: 3}}"));
        Assert.That(material, Does.Contain("_GlowColor: {r: 1, g: 1, b: 1, a: 0.7}"));
        Assert.That(material, Does.Contain("_GlowRadius: 6"));
        Assert.That(material, Does.Contain("_GlowSoftness: 1.6"));
    }

    private static string FindBlock(string source, string blockPrefix, string marker)
    {
        int markerIndex = source.IndexOf(marker, StringComparison.Ordinal);
        Assert.That(markerIndex, Is.GreaterThanOrEqualTo(0), marker);

        int start = source.LastIndexOf(blockPrefix, markerIndex, StringComparison.Ordinal);
        int end = source.IndexOf("\n--- !u!", markerIndex, StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0));
        return source.Substring(start, (end < 0 ? source.Length : end) - start);
    }

    private static string ReadObjectId(string gameObjectBlock)
    {
        int start = gameObjectBlock.IndexOf("--- !u!1 &", StringComparison.Ordinal) + "--- !u!1 &".Length;
        int end = gameObjectBlock.IndexOf('\n', start);
        return gameObjectBlock.Substring(start, end - start).Trim();
    }

    private static string FindComponentBlock(string scene, string gameObjectId, string scriptGuid)
    {
        string ownerMarker = $"  m_GameObject: {{fileID: {gameObjectId}}}";
        int searchIndex = 0;
        while ((searchIndex = scene.IndexOf(ownerMarker, searchIndex, StringComparison.Ordinal)) >= 0)
        {
            int start = scene.LastIndexOf("--- !u!114 &", searchIndex, StringComparison.Ordinal);
            int end = scene.IndexOf("\n--- !u!", searchIndex, StringComparison.Ordinal);
            if (start >= 0)
            {
                string block = scene.Substring(start, (end < 0 ? scene.Length : end) - start);
                if (block.Contains($"guid: {scriptGuid}"))
                    return block;
            }

            searchIndex += ownerMarker.Length;
        }

        return null;
    }

    private static string FindOwnedBlock(string scene, string gameObjectId, string blockPrefix)
    {
        string ownerMarker = $"  m_GameObject: {{fileID: {gameObjectId}}}";
        int markerIndex = scene.IndexOf(ownerMarker, StringComparison.Ordinal);
        Assert.That(markerIndex, Is.GreaterThanOrEqualTo(0), ownerMarker);

        int start = scene.LastIndexOf(blockPrefix, markerIndex, StringComparison.Ordinal);
        int end = scene.IndexOf("\n--- !u!", markerIndex, StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0));
        return scene.Substring(start, (end < 0 ? scene.Length : end) - start);
    }
}

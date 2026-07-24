using System.IO;
using NUnit.Framework;

public class MagicCircleHoverPulseTests
{
    private const string PrefabPath = "Assets/Project/Art/VFX/Vfx_magic_circle.prefab";
    private const string ScriptPath = "Assets/Project/Scripts/Art/MagicCircleHoverPulse.cs";
    private const string LobbyScenePath = "Assets/Project/Scenes/YDM/Lobby.unity";
    private const string ScriptGuid = "c5ec8c1e9ba04b6f80f677996e39d394";

    [Test]
    public void MagicCirclePrefab_UsesFixedBaseAlpha150AndHoverAlpha250()
    {
        string prefab = File.ReadAllText(PrefabPath);
        string script = File.ReadAllText(ScriptPath);

        Assert.That(script, Does.Contain("baseAlphaByte = 150"));
        Assert.That(script, Does.Contain("hoverAlphaByte = 250"));
        Assert.That(script, Does.Contain("baseRedByte = 100"));
        Assert.That(script, Does.Contain("baseGreenByte = 100"));
        Assert.That(script, Does.Contain("baseBlueByte = 100"));
        Assert.That(script, Does.Contain("hoverRedByte = 100"));
        Assert.That(script, Does.Contain("hoverGreenByte = 150"));
        Assert.That(script, Does.Contain("hoverBlueByte = 200"));

        Assert.That(prefab, Does.Contain("key0: {r: 0.3921569, g: 0.3921569, b: 0.3921569, a: 0.5882353}"));
        Assert.That(prefab, Does.Contain("key1: {r: 0.3921569, g: 0.3921569, b: 0.3921569, a: 0.5882353}"));
        Assert.That(prefab, Does.Contain("key2: {r: 0.3921569, g: 0.3921569, b: 0.3921569, a: 0.5882353}"));
        Assert.That(prefab, Does.Contain($"guid: {ScriptGuid}"));
        Assert.That(prefab, Does.Contain("hoverSourceObject: {fileID: 9093727058551169370}"));
        Assert.That(prefab, Does.Contain("hoverSourceObjects: []"));
        Assert.That(prefab, Does.Contain("baseAlphaByte: 150"));
        Assert.That(prefab, Does.Contain("hoverAlphaByte: 250"));
        Assert.That(prefab, Does.Contain("baseRedByte: 100"));
        Assert.That(prefab, Does.Contain("baseGreenByte: 100"));
        Assert.That(prefab, Does.Contain("baseBlueByte: 100"));
        Assert.That(prefab, Does.Contain("hoverRedByte: 100"));
        Assert.That(prefab, Does.Contain("hoverGreenByte: 150"));
        Assert.That(prefab, Does.Contain("hoverBlueByte: 200"));
    }

    [Test]
    public void MagicCirclePulseScript_GraduallyMovesPeakTowardHoverAlphaAndColor()
    {
        string script = File.ReadAllText(ScriptPath);

        Assert.That(script, Does.Contain("Mathf.MoveTowards"));
        Assert.That(script, Does.Contain("Vector3.MoveTowards"));
        Assert.That(script, Does.Contain("IsHovering"));
        Assert.That(script, Does.Contain("hoverSourceObjects"));
        Assert.That(script, Does.Contain("AnyHoverSourceActive"));
        Assert.That(script, Does.Contain("ApplyColorAndAlphaGradient"));
        Assert.That(script, Does.Contain("ByteToColor"));
    }

    [Test]
    public void LobbyMagicCircle_UsesStatueHoverObjectAsAdditionalHoverSource()
    {
        string scene = File.ReadAllText(LobbyScenePath);

        Assert.That(scene, Does.Contain("propertyPath: hoverSourceObjects.Array.size"));
        Assert.That(scene, Does.Contain("propertyPath: hoverSourceObjects.Array.data[0]"));
        Assert.That(scene, Does.Contain("objectReference: {fileID: 1460168266}"));
    }
}

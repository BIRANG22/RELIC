using System.IO;
using NUnit.Framework;

public sealed class RestRoomVfxOverlayCameraTests
{
    private const string ScriptPath = "Assets/Project/Scripts/Gameplay/Scene/Battle/Background/RestRoomVfxOverlayCamera.cs";
    private const string PrefabPath = "Assets/Project/PrefabsR/Battle/BackGround/Share_Restroom.prefab";

    [Test]
    public void OverlayCamera_UsesForwardRendererAndOnlyVfxLayer()
    {
        Assert.That(File.Exists(ScriptPath), Is.True);

        string source = File.ReadAllText(ScriptPath);

        Assert.That(source, Does.Contain("camera.cullingMask = LayerMask.GetMask(\"VFX\");"));
        Assert.That(source, Does.Contain("cameraData.SetRenderer(ForwardRendererIndex);"));
        Assert.That(source, Does.Contain("CameraClearFlags.Depth"));
    }

    [Test]
    public void RestRoomPrefab_UsesAdditiveLightAndOverlayCamera()
    {
        string prefab = File.ReadAllText(PrefabPath);

        Assert.That(prefab, Does.Contain("guid: 6ca77b7c71ab4bd9aa9c7c0ff7f1bb32"));
        Assert.That(prefab, Does.Contain("m_BlendStyleIndex: 1"));
    }
}

using System.IO;
using NUnit.Framework;
using UnityEngine;

public sealed class BattleCameraOwnershipBoundaryTests
{
    [Test]
    public void MonsterInfoUi_DoesNotOwnOrWriteMainCamera()
    {
        string source = File.ReadAllText(
            "Assets/Project/Scripts/BattleMonsterInfoCanvasUI.cs");

        Assert.That(source, Does.Not.Contain("private Camera mainCamera"));
        Assert.That(source, Does.Not.Contain("mainCamera.transform.position"));
        Assert.That(source, Does.Not.Contain("CameraMoveRoutine"));
        Assert.That(source, Does.Contain("FocusMonsterInfoForGridIndex"));
        Assert.That(source, Does.Contain("ReturnDefaultFromMonsterInfoFocus"));
    }

    [Test]
    public void DissolveReveal_DoesNotIssueCameraFocusRequests()
    {
        string source = File.ReadAllText(
            "Assets/Project/Scripts/UI/Battle/Canvas/UIDissolveReveal.cs");

        Assert.That(source, Does.Not.Contain("FocusMonsterInfoCamera"));
        Assert.That(source, Does.Not.Contain("ReturnMonsterInfoCameraFocus"));
    }

    [Test]
    public void BattleCameraController_RecognizesItsCameraTransform()
    {
        GameObject cameraObject = new GameObject("Camera", typeof(Camera));
        BattleCameraController controller = cameraObject.AddComponent<BattleCameraController>();
        GameObject otherObject = new GameObject("Other");

        try
        {
            Assert.That(controller.UsesCameraTransform(cameraObject.transform), Is.True);
            Assert.That(controller.UsesCameraTransform(otherObject.transform), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(otherObject);
            Object.DestroyImmediate(cameraObject);
        }
    }


    [Test]
    public void SpriteSequence_ReleasesCinematicSessionWhenPoseIsKept()
    {
        string source = File.ReadAllText(
            "Assets/Project/Scripts/Art/SpriteTransformSequence.cs");

        Assert.That(source, Does.Contain("EndCinematicCameraKeepingCurrentPose"));
    }
}

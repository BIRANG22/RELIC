using System.IO;
using System.Reflection;
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

    [Test]
    public void EndCinematicCameraKeepingCurrentPose_RemovesPresentationOffsetsAndMotion()
    {
        GameObject cameraObject = new GameObject("CinematicCamera", typeof(Camera));
        BattleCameraController controller = cameraObject.AddComponent<BattleCameraController>();

        try
        {
            cameraObject.transform.position = new Vector3(2f, 3f, -10f);
            SetPrivate(controller, "targetCamera", cameraObject.GetComponent<Camera>());
            SetPrivate(controller, "hasCinematicCameraState", true);
            SetPrivate(controller, "hasAppliedExternalShake", true);
            SetPrivate(controller, "appliedExternalShakeOffset", new Vector3(0.25f, -0.2f, 0f));
            SetPrivate(controller, "externalShakeOffset", new Vector3(0.25f, -0.2f, 0f));
            SetPrivate(controller, "movementFocusVelocity", new Vector3(1f, 2f, 0f));

            controller.EndCinematicCameraKeepingCurrentPose();

            Assert.That(cameraObject.transform.position, Is.EqualTo(new Vector3(1.75f, 3.2f, -10f)));
            Assert.That(GetPrivate<bool>(controller, "hasCinematicCameraState"), Is.False);
            Assert.That(GetPrivate<Vector3>(controller, "externalShakeOffset"), Is.EqualTo(Vector3.zero));
            Assert.That(GetPrivate<Vector3>(controller, "movementFocusVelocity"), Is.EqualTo(Vector3.zero));
        }
        finally
        {
            Object.DestroyImmediate(cameraObject);
        }
    }

    private static void SetPrivate(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, name);
        field.SetValue(target, value);
    }

    private static T GetPrivate<T>(object target, string name)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, name);
        return (T)field.GetValue(target);
    }
}

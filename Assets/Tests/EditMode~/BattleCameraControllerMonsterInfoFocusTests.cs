using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class BattleCameraControllerMonsterInfoFocusTests
{
    [Test]
    public void FocusMonsterInfo_MovesCameraTowardTargetWithConfiguredLightZoom()
    {
        CameraFixture fixture = CreateFixture();
        GameObject target = new("MonsterInfoFocus_Target");
        target.transform.position = new Vector3(3f, 4f, 0f);

        try
        {
            SetPrivateField(fixture.Controller, "monsterInfoFocusDuration", 0f);
            SetPrivateField(fixture.Controller, "monsterInfoFocusOffset", new Vector2(0.5f, 0.25f));
            SetPrivateField(fixture.Controller, "useMonsterInfoFocusZ", true);
            SetPrivateField(fixture.Controller, "useFixedMonsterInfoFocusZ", false);
            SetPrivateField(fixture.Controller, "monsterInfoFocusZOffset", 2f);
            SetPrivateField(fixture.Controller, "useMonsterInfoFocusOrthographicSize", false);

            fixture.Controller.FocusMonsterInfo(target.transform);

            Assert.That(fixture.Camera.transform.position, Is.EqualTo(new Vector3(3.5f, 4.25f, -18f)));
            Assert.That(fixture.Controller.IsMonsterInfoFocusActive, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(target);
            fixture.Destroy();
        }
    }

    [Test]
    public void ReturnDefaultFromMonsterInfoFocus_ReturnsCameraToCapturedDefault()
    {
        CameraFixture fixture = CreateFixture();
        GameObject target = new("MonsterInfoFocus_Target");
        target.transform.position = new Vector3(-2f, 1f, 0f);

        try
        {
            SetPrivateField(fixture.Controller, "monsterInfoFocusDuration", 0f);
            SetPrivateField(fixture.Controller, "monsterInfoReturnDuration", 0f);
            SetPrivateField(fixture.Controller, "monsterInfoFocusZOffset", 2f);

            fixture.Controller.FocusMonsterInfo(target.transform);
            fixture.Controller.ReturnDefaultFromMonsterInfoFocus();

            Assert.That(fixture.Camera.transform.position, Is.EqualTo(new Vector3(0f, 0f, -20f)));
            Assert.That(fixture.Camera.orthographicSize, Is.EqualTo(5f));
            Assert.That(fixture.Controller.IsMonsterInfoFocusActive, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(target);
            fixture.Destroy();
        }
    }

    [Test]
    public void ForceReturnMapImmediate_RestoresCameraToMapPositionAfterFocus()
    {
        CameraFixture fixture = CreateFixture();
        GameObject target = new("MonsterInfoFocus_Target");
        target.transform.position = new Vector3(3f, 4f, 0f);

        try
        {
            SetPrivateField(fixture.Controller, "monsterInfoFocusDuration", 0f);
            SetPrivateField(fixture.Controller, "monsterInfoFocusZOffset", 2f);
            SetPrivateField(fixture.Controller, "mapPosition", new Vector3(0f, 0f, -20f));

            fixture.Controller.FocusMonsterInfo(target.transform);
            fixture.Camera.transform.rotation = Quaternion.Euler(0f, 0f, 7f);

            fixture.Controller.ForceReturnMapImmediate();

            Assert.That(fixture.Camera.transform.position, Is.EqualTo(new Vector3(0f, 0f, -20f)));
            Assert.That(fixture.Camera.transform.rotation, Is.EqualTo(Quaternion.identity));
            Assert.That(fixture.Camera.orthographicSize, Is.EqualTo(5f));
            Assert.That(fixture.Controller.IsMonsterInfoFocusActive, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(target);
            fixture.Destroy();
        }
    }

    [Test]
    public void FocusMonsterInfoWithPanelSide_LeftPanelMovesCameraLeftSoMonsterAppearsRight()
    {
        CameraFixture fixture = CreateFixture();
        GameObject target = new("MonsterInfoFocus_Target");
        target.transform.position = new Vector3(3f, 4f, 0f);

        try
        {
            SetPrivateField(fixture.Controller, "monsterInfoFocusDuration", 0f);
            SetPrivateField(fixture.Controller, "monsterInfoFocusOffset", new Vector2(0f, 0.25f));
            SetPrivateField(fixture.Controller, "monsterInfoFocusSideOffset", 0.75f);
            SetPrivateField(fixture.Controller, "useMonsterInfoFocusZ", true);
            SetPrivateField(fixture.Controller, "useFixedMonsterInfoFocusZ", false);
            SetPrivateField(fixture.Controller, "monsterInfoFocusZOffset", 2f);
            SetPrivateField(fixture.Controller, "useMonsterInfoFocusOrthographicSize", false);

            fixture.Controller.FocusMonsterInfoWithPanelSide(target.transform, true);

            Assert.That(fixture.Camera.transform.position, Is.EqualTo(new Vector3(2.25f, 4.25f, -18f)));
        }
        finally
        {
            Object.DestroyImmediate(target);
            fixture.Destroy();
        }
    }

    [Test]
    public void FocusMonsterInfoWithPanelSide_RightPanelMovesCameraRightSoMonsterAppearsLeft()
    {
        CameraFixture fixture = CreateFixture();
        GameObject target = new("MonsterInfoFocus_Target");
        target.transform.position = new Vector3(3f, 4f, 0f);

        try
        {
            SetPrivateField(fixture.Controller, "monsterInfoFocusDuration", 0f);
            SetPrivateField(fixture.Controller, "monsterInfoFocusOffset", new Vector2(0f, 0.25f));
            SetPrivateField(fixture.Controller, "monsterInfoFocusSideOffset", 0.75f);
            SetPrivateField(fixture.Controller, "useMonsterInfoFocusZ", true);
            SetPrivateField(fixture.Controller, "useFixedMonsterInfoFocusZ", false);
            SetPrivateField(fixture.Controller, "monsterInfoFocusZOffset", 2f);
            SetPrivateField(fixture.Controller, "useMonsterInfoFocusOrthographicSize", false);

            fixture.Controller.FocusMonsterInfoWithPanelSide(target.transform, false);

            Assert.That(fixture.Camera.transform.position, Is.EqualTo(new Vector3(3.75f, 4.25f, -18f)));
        }
        finally
        {
            Object.DestroyImmediate(target);
            fixture.Destroy();
        }
    }

    private static CameraFixture CreateFixture()
    {
        GameObject cameraObject = new("MonsterInfoFocus_Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = false;
        camera.orthographicSize = 5f;
        cameraObject.transform.position = new Vector3(0f, 0f, -20f);

        GameObject controllerObject = new("MonsterInfoFocus_Controller");
        controllerObject.SetActive(false);
        BattleCameraController controller = controllerObject.AddComponent<BattleCameraController>();
        SetPrivateField(controller, "targetCamera", camera);
        controllerObject.SetActive(true);

        return new CameraFixture(cameraObject, controllerObject, camera, controller);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(target, value);
    }

    private readonly struct CameraFixture
    {
        public CameraFixture(
            GameObject cameraObject,
            GameObject controllerObject,
            Camera camera,
            BattleCameraController controller)
        {
            CameraObject = cameraObject;
            ControllerObject = controllerObject;
            Camera = camera;
            Controller = controller;
        }

        public readonly GameObject CameraObject;
        public readonly GameObject ControllerObject;
        public readonly Camera Camera;
        public readonly BattleCameraController Controller;

        public void Destroy()
        {
            Object.DestroyImmediate(ControllerObject);
            Object.DestroyImmediate(CameraObject);
        }
    }
}

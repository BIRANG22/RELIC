using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class ArabellaPhaseTransitionCameraTests
{
    private GameObject cameraObject;
    private GameObject targetObject;
    private Camera targetCamera;
    private BattleCameraController controller;

    [SetUp]
    public void SetUp()
    {
        cameraObject = new GameObject("Arabella Phase Camera Test");
        targetCamera = cameraObject.AddComponent<Camera>();
        targetCamera.orthographic = true;
        targetCamera.orthographicSize = 5f;
        cameraObject.transform.position = new Vector3(0f, 0f, -20f);
        controller = cameraObject.AddComponent<BattleCameraController>();

        targetObject = new GameObject("Arabella");
        targetObject.transform.position = new Vector3(4f, 2f, 0f);

        SetField("targetCamera", targetCamera);
        SetField("enableArabellaPhaseTransitionCamera", true);
        SetField("arabellaPhaseFirstOffset", new Vector2(1f, 2f));
        SetField("arabellaPhaseFirstZPosition", -15f);
        SetField("arabellaPhaseFirstOrthographicSize", 4f);
        SetField("arabellaPhaseFirstDuration", 0f);
        SetField("enableArabellaPhaseSecondStep", true);
        SetField("arabellaPhaseSecondOffset", new Vector2(-2f, 3f));
        SetField("arabellaPhaseSecondZPosition", -11f);
        SetField("arabellaPhaseSecondOrthographicSize", 3f);
        SetField("arabellaPhaseSecondDuration", 0f);
        SetField("arabellaPhaseStepHoldDuration", 0f);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(targetObject);
        Object.DestroyImmediate(cameraObject);
    }

    [Test]
    public void PlayArabellaPhaseTransitionCamera_WithSecondStep_EndsAtSecondFraming()
    {
        RunToCompletion(InvokeRoutine("PlayArabellaPhaseTransitionCameraSteps", targetObject.transform));

        Assert.That(cameraObject.transform.position, Is.EqualTo(new Vector3(2f, 5f, -11f)));
        Assert.That(targetCamera.orthographicSize, Is.EqualTo(3f));
    }

    [Test]
    public void PlayArabellaPhaseTransitionCamera_WithoutSecondStep_EndsAtFirstFraming()
    {
        SetField("enableArabellaPhaseSecondStep", false);

        RunToCompletion(InvokeRoutine("PlayArabellaPhaseTransitionCameraSteps", targetObject.transform));

        Assert.That(cameraObject.transform.position, Is.EqualTo(new Vector3(5f, 4f, -15f)));
        Assert.That(targetCamera.orthographicSize, Is.EqualTo(4f));
    }

    [Test]
    public void PlayArabellaPhaseTransitionCamera_WithNullTarget_LeavesCameraUnchanged()
    {
        Vector3 originalPosition = cameraObject.transform.position;

        RunToCompletion(InvokeRoutine("PlayArabellaPhaseTransitionCameraSteps", null));

        Assert.That(cameraObject.transform.position, Is.EqualTo(originalPosition));
        Assert.That(targetCamera.orthographicSize, Is.EqualTo(5f));
    }

    [Test]
    public void ReturnFromArabellaPhaseTransitionCamera_ClearsDirectionalYaw()
    {
        SetField("arabellaPhaseReturnDuration", 0f);
        cameraObject.transform.rotation = Quaternion.Euler(0f, 5f, 0f);

        RunToCompletion(InvokeRoutine("ReturnFromArabellaPhaseTransitionCameraSteps"));

        Assert.That(Mathf.DeltaAngle(0f, cameraObject.transform.eulerAngles.y), Is.EqualTo(0f).Within(0.001f));
    }

    private void SetField(string fieldName, object value)
    {
        FieldInfo field = typeof(BattleCameraController).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing field: {fieldName}");
        field.SetValue(controller, value);
    }

    private IEnumerator InvokeRoutine(string methodName, params object[] arguments)
    {
        MethodInfo method = typeof(BattleCameraController).GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, $"Missing method: {methodName}");
        return (IEnumerator)method.Invoke(controller, arguments);
    }

    private static void RunToCompletion(IEnumerator root)
    {
        Stack<IEnumerator> routines = new Stack<IEnumerator>();
        routines.Push(root);

        while (routines.Count > 0)
        {
            IEnumerator current = routines.Peek();
            if (!current.MoveNext())
            {
                routines.Pop();
                continue;
            }

            if (current.Current is IEnumerator nested)
                routines.Push(nested);
        }
    }
}

using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class BattleCameraExplicitMovementFocusTests
{
    private GameObject cameraObject;
    private GameObject attackerObject;
    private GameObject targetObject;

    [SetUp]
    public void SetUp()
    {
        cameraObject = new GameObject("BattleCameraExplicitMovementFocusTests.Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        BattleCameraController controller = cameraObject.AddComponent<BattleCameraController>();

        SetPrivateField(controller, "targetCamera", camera);
        SetPrivateField(controller, "turnExecutionEventActive", true);

        attackerObject = new GameObject("Attacker");
        targetObject = new GameObject("KnockbackTarget");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(targetObject);
        Object.DestroyImmediate(attackerObject);
        Object.DestroyImmediate(cameraObject);
    }

    [Test]
    public void TransferDirectGridMovementFocus_ReplacesAttackerWithKnockbackTargetExactlyOnce()
    {
        BattleCameraController controller = cameraObject.GetComponent<BattleCameraController>();

        controller.BeginZoomFollowTarget(attackerObject.transform);
        bool transferred = controller.TransferDirectGridMovementFocus(
            attackerObject.transform,
            targetObject.transform);
        controller.EndDirectGridMovement(attackerObject.transform);

        Assert.That(transferred, Is.True);
        Assert.That(GetFocusedUnit(controller), Is.SameAs(targetObject.transform));

        controller.EndDirectGridMovement(targetObject.transform);
        Assert.That(GetFocusedUnit(controller), Is.Null);
    }

    [Test]
    public void TransferDirectGridMovementFocus_RejectsStaleOwner()
    {
        BattleCameraController controller = cameraObject.GetComponent<BattleCameraController>();
        GameObject staleOwner = new GameObject("StaleOwner");

        try
        {
            controller.BeginDirectGridMovement(attackerObject.transform);

            bool transferred = controller.TransferDirectGridMovementFocus(
                staleOwner.transform,
                targetObject.transform);

            Assert.That(transferred, Is.False);
            Assert.That(GetFocusedUnit(controller), Is.SameAs(attackerObject.transform));
        }
        finally
        {
            Object.DestroyImmediate(staleOwner);
        }
    }

    private static Transform GetFocusedUnit(BattleCameraController controller)
    {
        return (Transform)typeof(BattleCameraController)
            .GetField("gridMovementFocusedUnit", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(controller);
    }

    private static void SetPrivateField<T>(BattleCameraController controller, string fieldName, T value)
    {
        typeof(BattleCameraController)
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(controller, value);
    }
}

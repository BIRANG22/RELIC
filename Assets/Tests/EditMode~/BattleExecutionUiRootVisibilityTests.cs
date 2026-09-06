using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class BattleExecutionUiRootVisibilityTests
{
    [Test]
    public void SetBattleExecutionUiVisible_TogglesConfiguredPlayerHudAndMenuRoots()
    {
        GameObject executorObject = new("BattleTurnExecutorUiVisibility");
        GameObject playerHudRoot = new("ConfiguredPlayerHUD_Root");
        GameObject menuRoot = new("ConfiguredMenuRoot");

        try
        {
            BattleTurnExecutor executor = executorObject.AddComponent<BattleTurnExecutor>();

            SetPrivateField(executor, "playerHudRoot", playerHudRoot);
            SetPrivateField(executor, "menuRoot", menuRoot);

            InvokePrivateMethod(executor, "SetBattleExecutionUiVisible", false);

            Assert.That(playerHudRoot.activeSelf, Is.False);
            Assert.That(menuRoot.activeSelf, Is.False);

            InvokePrivateMethod(executor, "SetBattleExecutionUiVisible", true);

            Assert.That(playerHudRoot.activeSelf, Is.True);
            Assert.That(menuRoot.activeSelf, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(executorObject);
            Object.DestroyImmediate(playerHudRoot);
            Object.DestroyImmediate(menuRoot);
        }
    }

    [Test]
    public void SetBattleExecutionUiVisible_AutoFindsNamedRootsWhenFieldsAreEmpty()
    {
        GameObject executorObject = new("BattleTurnExecutorUiAutoFind");
        GameObject firstPlayerHudRoot = new("PlayerHUD_Root");
        GameObject secondPlayerHudRoot = new("PlayerHUD_Root");
        GameObject menuRoot = new("MenuRoot");

        try
        {
            BattleTurnExecutor executor = executorObject.AddComponent<BattleTurnExecutor>();

            InvokePrivateMethod(executor, "SetBattleExecutionUiVisible", false);

            Assert.That(firstPlayerHudRoot.activeSelf, Is.False);
            Assert.That(secondPlayerHudRoot.activeSelf, Is.False);
            Assert.That(menuRoot.activeSelf, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(executorObject);
            Object.DestroyImmediate(firstPlayerHudRoot);
            Object.DestroyImmediate(secondPlayerHudRoot);
            Object.DestroyImmediate(menuRoot);
        }
    }

    [Test]
    public void RestoreBattleExecutionUiAfterRoomEnd_ReactivatesSuppressedRoots()
    {
        GameObject executorObject = new("BattleTurnExecutorUiRestore");
        GameObject playerHudRoot = new("ConfiguredPlayerHUD_Root");
        GameObject menuRoot = new("ConfiguredMenuRoot");

        try
        {
            BattleTurnExecutor executor = executorObject.AddComponent<BattleTurnExecutor>();

            SetPrivateField(executor, "playerHudRoot", playerHudRoot);
            SetPrivateField(executor, "menuRoot", menuRoot);
            SetPrivateField(executor, "battleExecutionUiSuppressed", true);
            playerHudRoot.SetActive(false);
            menuRoot.SetActive(false);

            executor.RestoreBattleExecutionUiAfterRoomEnd();

            Assert.That(playerHudRoot.activeSelf, Is.True);
            Assert.That(menuRoot.activeSelf, Is.True);
            Assert.That(
                GetPrivateField<bool>(executor, "battleExecutionUiSuppressed"),
                Is.False);
        }
        finally
        {
            Object.DestroyImmediate(executorObject);
            Object.DestroyImmediate(playerHudRoot);
            Object.DestroyImmediate(menuRoot);
        }
    }

    [Test]
    public void RestoreBattleExecutionUiAfterRoomEnd_DoesNotOverrideUnsuppressedRoots()
    {
        GameObject executorObject = new("BattleTurnExecutorUiNoRestore");
        GameObject playerHudRoot = new("ConfiguredPlayerHUD_Root");
        GameObject menuRoot = new("ConfiguredMenuRoot");

        try
        {
            BattleTurnExecutor executor = executorObject.AddComponent<BattleTurnExecutor>();

            SetPrivateField(executor, "playerHudRoot", playerHudRoot);
            SetPrivateField(executor, "menuRoot", menuRoot);
            playerHudRoot.SetActive(false);
            menuRoot.SetActive(false);

            executor.RestoreBattleExecutionUiAfterRoomEnd();

            Assert.That(playerHudRoot.activeSelf, Is.False);
            Assert.That(menuRoot.activeSelf, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(executorObject);
            Object.DestroyImmediate(playerHudRoot);
            Object.DestroyImmediate(menuRoot);
        }
    }

    private static void SetPrivateField<TValue>(
        BattleTurnExecutor executor,
        string fieldName,
        TValue value)
    {
        FieldInfo field = typeof(BattleTurnExecutor).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, $"{fieldName} field is missing.");
        field.SetValue(executor, value);
    }

    private static TValue GetPrivateField<TValue>(
        BattleTurnExecutor executor,
        string fieldName)
    {
        FieldInfo field = typeof(BattleTurnExecutor).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, $"{fieldName} field is missing.");
        return (TValue)field.GetValue(executor);
    }

    private static void InvokePrivateMethod(
        BattleTurnExecutor executor,
        string methodName,
        params object[] args)
    {
        MethodInfo method = typeof(BattleTurnExecutor).GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, $"{methodName} method is missing.");
        method.Invoke(executor, args);
    }
}

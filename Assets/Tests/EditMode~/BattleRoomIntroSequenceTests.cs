using NUnit.Framework;
using System.Reflection;
using UnityEngine;

public class BattleRoomIntroSequenceTests
{
    private sealed class TestIntroSequence : BattleRoomIntroSequence
    {
        public void CompleteForTest()
        {
            MarkCompleted();
        }

        public void ResetForTest()
        {
            ResetCompletion();
        }
    }

    [Test]
    public void AnimationSequence_DefaultPostSequenceDelayIsOneSecond()
    {
        GameObject gameObject = new("AnimationSequence");
        gameObject.SetActive(false);

        try
        {
            AnimationSequence sequence = gameObject.AddComponent<AnimationSequence>();

            Assert.That(sequence.PostSequenceDelay, Is.EqualTo(1f));
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void MarkCompleted_RaisesCompletedOnlyOnce()
    {
        GameObject gameObject = new("IntroSequence");

        try
        {
            TestIntroSequence sequence = gameObject.AddComponent<TestIntroSequence>();
            int completedCount = 0;
            sequence.Completed += () => completedCount++;

            sequence.CompleteForTest();
            sequence.CompleteForTest();

            Assert.That(sequence.IsCompleted, Is.True);
            Assert.That(completedCount, Is.EqualTo(1));
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void ResetCompletion_AllowsNextCompletionNotification()
    {
        GameObject gameObject = new("IntroSequence");

        try
        {
            TestIntroSequence sequence = gameObject.AddComponent<TestIntroSequence>();
            int completedCount = 0;
            sequence.Completed += () => completedCount++;

            sequence.CompleteForTest();
            sequence.ResetForTest();
            sequence.CompleteForTest();

            Assert.That(sequence.IsCompleted, Is.True);
            Assert.That(completedCount, Is.EqualTo(2));
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void FindFirst_ReturnsActiveChildSequence()
    {
        GameObject root = new("BattleRoom");
        GameObject child = new("BossIntro");
        child.transform.SetParent(root.transform, false);
        TestIntroSequence expected = child.AddComponent<TestIntroSequence>();
        try
        {
            IBattleRoomIntroSequence result = BattleRoomIntroSequenceUtility.FindFirst(root);

            Assert.That(result, Is.SameAs(expected));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void FindFirst_ReturnsTimedObjectRevealSequence()
    {
        GameObject root = new("SharedRoomRoot");
        GameObject child = new("St1BossRevealSequence");
        child.transform.SetParent(root.transform, false);
        TimedObjectRevealSequence expected = child.AddComponent<TimedObjectRevealSequence>();

        try
        {
            IBattleRoomIntroSequence result = BattleRoomIntroSequenceUtility.FindFirst(root);

            Assert.That(result, Is.SameAs(expected));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void BattleSceneController_WaitsForSharedBossIntroAndSuppressesBattleUi()
    {
        GameObject controllerObject = new("BattleSceneController");
        GameObject battleRoom = new("BattleRoom");
        GameObject sharedRoot = new("SharedRoomRoot");
        GameObject sequenceObject = new("BossIntroSequence");
        GameObject executorObject = new("BattleTurnExecutor");
        GameObject playerHudRoot = new("PlayerHUD_Root");
        GameObject menuRoot = new("MenuRoot");
        sequenceObject.transform.SetParent(sharedRoot.transform, false);
        executorObject.transform.SetParent(battleRoom.transform, false);
        TestIntroSequence sequence = sequenceObject.AddComponent<TestIntroSequence>();
        BattleTurnExecutor executor = executorObject.AddComponent<BattleTurnExecutor>();
        playerHudRoot.SetActive(true);
        menuRoot.SetActive(true);

        try
        {
            BattleSceneController controller = controllerObject.AddComponent<BattleSceneController>();
            SetPrivateField(controller, "battleRoom", battleRoom);
            SetPrivateField(controller, "sharedRoomRoot", sharedRoot);
            SetPrivateField(controller, "pendingBattleRoomUsesBossIntro", true);
            SetPrivateField(executor, "playerHudRoot", playerHudRoot);
            SetPrivateField(executor, "menuRoot", menuRoot);

            InvokePrivateMethod(controller, "RequestBattleRoomLoadOnce");

            Assert.That(BattleSceneController.IsBattleRoomIntroPlaying, Is.True);
            Assert.That(playerHudRoot.activeSelf, Is.False);
            Assert.That(menuRoot.activeSelf, Is.False);
            Assert.That(sequence.IsCompleted, Is.False);
        }
        finally
        {
            InvokePrivateStaticMethod(
                typeof(BattleSceneController),
                "SetBattleRoomIntroPlaying",
                false);
            Object.DestroyImmediate(controllerObject);
            Object.DestroyImmediate(battleRoom);
            Object.DestroyImmediate(sharedRoot);
            Object.DestroyImmediate(playerHudRoot);
            Object.DestroyImmediate(menuRoot);
        }
    }

    [Test]
    public void FindFirst_IgnoresInactiveChildSequence()
    {
        GameObject root = new("BattleRoom");
        GameObject child = new("InactiveBossIntro");
        child.transform.SetParent(root.transform, false);
        child.AddComponent<TestIntroSequence>();
        child.SetActive(false);

        try
        {
            Assert.That(BattleRoomIntroSequenceUtility.FindFirst(root), Is.Null);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void FindFirst_ReturnsNullWhenRoomHasNoIntroSequence()
    {
        GameObject root = new("BattleRoom");

        try
        {
            Assert.That(BattleRoomIntroSequenceUtility.FindFirst(root), Is.Null);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void LoadGate_RequestWithoutSequence_LoadsImmediately()
    {
        BattleRoomIntroLoadGate gate = new();
        int loadCount = 0;

        gate.Request(null, () => loadCount++);

        Assert.That(loadCount, Is.EqualTo(1));
    }

    [Test]
    public void LoadGate_WaitsForSequenceAndLoadsOnlyOnce()
    {
        GameObject gameObject = new("IntroSequence");

        try
        {
            TestIntroSequence sequence = gameObject.AddComponent<TestIntroSequence>();
            BattleRoomIntroLoadGate gate = new();
            int loadCount = 0;

            gate.Request(sequence, () => loadCount++);
            Assert.That(loadCount, Is.Zero);

            sequence.CompleteForTest();
            sequence.CompleteForTest();

            Assert.That(loadCount, Is.EqualTo(1));
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void LoadGate_CancelIgnoresPreviousSequenceCompletion()
    {
        GameObject gameObject = new("IntroSequence");

        try
        {
            TestIntroSequence sequence = gameObject.AddComponent<TestIntroSequence>();
            BattleRoomIntroLoadGate gate = new();
            int loadCount = 0;

            gate.Request(sequence, () => loadCount++);
            gate.Cancel();
            sequence.CompleteForTest();

            Assert.That(loadCount, Is.Zero);
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void LoadGate_ReRequestAfterCompletion_LoadsImmediately()
    {
        GameObject gameObject = new("IntroSequence");

        try
        {
            TestIntroSequence sequence = gameObject.AddComponent<TestIntroSequence>();
            sequence.CompleteForTest();
            BattleRoomIntroLoadGate gate = new();
            int loadCount = 0;

            gate.Request(sequence, () => loadCount++);

            Assert.That(loadCount, Is.EqualTo(1));
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, $"Missing field: {fieldName}");
        field.SetValue(target, value);
    }

    private static void InvokePrivateMethod(object target, string methodName, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, $"Missing method: {methodName}");
        method.Invoke(target, args);
    }

    private static void InvokePrivateStaticMethod(
        System.Type type,
        string methodName,
        params object[] args)
    {
        MethodInfo method = type.GetMethod(
            methodName,
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, $"Missing method: {methodName}");
        method.Invoke(null, args);
    }
}

using NUnit.Framework;
using Relic.Gameplay.Data;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class BattleHitImpactFeedbackTests
{
    [Test]
    public void ResolveHorizontalDirection_UsesTargetSideWhenSeparated()
    {
        GameObject attacker = new("ImpactDirection_Attacker");
        GameObject target = new("ImpactDirection_Target");

        try
        {
            attacker.transform.position = new Vector3(0f, 0f, 0f);
            target.transform.position = new Vector3(2f, 0f, 0f);

            int direction = BattleHitImpactFeedback.ResolveHorizontalDirection(
                attacker.transform,
                target.transform,
                -1);

            Assert.That(direction, Is.EqualTo(1));
        }
        finally
        {
            Object.DestroyImmediate(attacker);
            Object.DestroyImmediate(target);
        }
    }

    [Test]
    public void ResolveHorizontalDirection_UsesFallbackWhenSameColumn()
    {
        GameObject attacker = new("ImpactDirection_Same_Attacker");
        GameObject target = new("ImpactDirection_Same_Target");

        try
        {
            attacker.transform.position = new Vector3(1f, 0f, 0f);
            target.transform.position = new Vector3(1f, 3f, 0f);

            int direction = BattleHitImpactFeedback.ResolveHorizontalDirection(
                attacker.transform,
                target.transform,
                -1);

            Assert.That(direction, Is.EqualTo(-1));
        }
        finally
        {
            Object.DestroyImmediate(attacker);
            Object.DestroyImmediate(target);
        }
    }

    [Test]
    public void DamagePlayer_ZeroDamageHitShowsZeroPopup()
    {
        GameObject cameraObject = new("DamageZero_MainCamera");
        GameObject playerObject = new("DamageZero_Player");

        try
        {
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";

            BattleCharacter character = playerObject.AddComponent<BattleCharacter>();
            character.Initialize(new CharacterRuntimeData
            {
                CharacterId = "DamageZero_Player",
                MaxHP = 10,
                CurrentHP = 10,
                CurrentShield = 0
            });

            BattleEffectUtility.DamagePlayer(character, 0);

            Assert.That(GameObject.Find("DamageText_0"), Is.Not.Null);
        }
        finally
        {
            DestroyIfExists("DamageText_0");
            DestroyIfExists("BattleDamageTextPopupUI_Auto");
            DestroyIfExists("BattleDamageTextCanvas_Auto");
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(cameraObject);
        }
    }

    [Test]
    public void DefaultHoldDurations_AreLongEnoughToReadAsSeparatePause()
    {
        GameObject feedbackObject = new("ImpactFeedback_HoldDuration");

        try
        {
            BattleHitImpactFeedback feedback =
                feedbackObject.AddComponent<BattleHitImpactFeedback>();

            Assert.That(
                GetPrivateFloat(feedback, "damageHitPushHoldDuration"),
                Is.GreaterThanOrEqualTo(0.08f));
            Assert.That(
                GetPrivateFloat(feedback, "statusPulseHoldDuration"),
                Is.GreaterThanOrEqualTo(0.08f));
        }
        finally
        {
            Object.DestroyImmediate(feedbackObject);
        }
    }

    [Test]
    public void HoldPauseRoutine_SlowsVfxUntilRoutineEnds()
    {
        IEnumerator routine = InvokePrivateHoldPauseRoutine(0.1f);

        try
        {
            Assert.That(BattleVfxPlaybackPauseController.IsGlobalPauseActive, Is.False);

            Assert.That(routine.MoveNext(), Is.True);

            Assert.That(BattleVfxPlaybackPauseController.IsGlobalPauseActive, Is.True);
        }
        finally
        {
            (routine as System.IDisposable)?.Dispose();
            BattleVfxPlaybackPauseController.ResumeAll();
        }

        Assert.That(BattleVfxPlaybackPauseController.IsGlobalPauseActive, Is.False);
    }

    private static void DestroyIfExists(string objectName)
    {
        GameObject gameObject = GameObject.Find(objectName);

        if (gameObject != null)
            Object.DestroyImmediate(gameObject);
    }

    private static float GetPrivateFloat(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, $"{fieldName} field is missing.");
        return (float)field.GetValue(target);
    }

    private static IEnumerator InvokePrivateHoldPauseRoutine(float duration)
    {
        MethodInfo method = typeof(BattleHitImpactFeedback).GetMethod(
            "WaitUnscaledWithVfxPause",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, "WaitUnscaledWithVfxPause method is missing.");
        return (IEnumerator)method.Invoke(null, new object[] { duration });
    }
}

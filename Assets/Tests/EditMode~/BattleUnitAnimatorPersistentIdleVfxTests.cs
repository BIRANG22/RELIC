using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class BattleUnitAnimatorPersistentIdleVfxTests
{
    private GameObject owner;
    private GameObject vfxPrefab;

    [TearDown]
    public void TearDown()
    {
        if (owner != null)
            Object.DestroyImmediate(owner);

        if (vfxPrefab != null)
            Object.DestroyImmediate(vfxPrefab);
    }

    [Test]
    public void RefreshPersistentIdleVfx_WhenFacingChanges_RecreatesVfxWithCurrentFacing()
    {
        BattleUnitAnimator animator = CreateAnimator();
        BattleUnitFacing facing = owner.GetComponent<BattleUnitFacing>();

        InvokeRefresh(animator);
        GameObject firstInstance = GetPrivateField<GameObject>(animator, "persistentIdleVfxInstance");

        facing.FaceRight(false);
        InvokeRefresh(animator);
        GameObject secondInstance = GetPrivateField<GameObject>(animator, "persistentIdleVfxInstance");

        Assert.That(secondInstance, Is.Not.SameAs(firstInstance));
        Assert.That(secondInstance.transform.localScale.x, Is.LessThan(0f));
    }

    [Test]
    public void PersistentIdleVfx_WhenSuspendedTwice_RestoresOnlyAfterFinalResume()
    {
        BattleUnitAnimator animator = CreateAnimator();
        InvokeRefresh(animator);

        animator.SuspendPersistentIdleVfx();
        animator.SuspendPersistentIdleVfx();
        Assert.That(GetPrivateField<GameObject>(animator, "persistentIdleVfxInstance"), Is.Null);

        animator.ResumePersistentIdleVfx();
        Assert.That(GetPrivateField<GameObject>(animator, "persistentIdleVfxInstance"), Is.Null);

        animator.ResumePersistentIdleVfx();
        Assert.That(GetPrivateField<GameObject>(animator, "persistentIdleVfxInstance"), Is.Not.Null);
    }

    [Test]
    public void PersistentIdleVfx_WhenReenabledWhileSuspended_StaysHiddenUntilResume()
    {
        BattleUnitAnimator animator = CreateAnimator();
        InvokeRefresh(animator);

        animator.SuspendPersistentIdleVfx();
        owner.SetActive(false);
        owner.SetActive(true);

        Assert.That(GetPrivateField<GameObject>(animator, "persistentIdleVfxInstance"), Is.Null);

        animator.ResumePersistentIdleVfx();
        Assert.That(GetPrivateField<GameObject>(animator, "persistentIdleVfxInstance"), Is.Not.Null);
    }

    private BattleUnitAnimator CreateAnimator()
    {
        owner = new GameObject("Arabella");
        owner.AddComponent<BattleUnitFacing>();
        BattleUnitAnimator animator = owner.AddComponent<BattleUnitAnimator>();
        vfxPrefab = new GameObject("IdleVfx");
        SetPrivateField(animator, "idleVfx", new BattleVfxEntry
        {
            prefab = vfxPrefab,
            renderMode = BattleVfxRenderMode.DirectWorldRenderer,
            flipType = VfxFlipType.None
        });
        return animator;
    }

    private static void InvokeRefresh(BattleUnitAnimator animator)
    {
        MethodInfo method = typeof(BattleUnitAnimator).GetMethod(
            "RefreshPersistentIdleVfx",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        method.Invoke(animator, null);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing field: {fieldName}");
        return (T)field.GetValue(target);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing field: {fieldName}");
        field.SetValue(target, value);
    }
}

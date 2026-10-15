using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class BattleUnitAnimatorDeathVfxTests
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
    public void PlayDead_WhenCalledMoreThanOnce_SpawnsDeathVfxOnlyOnce()
    {
        owner = new GameObject("Unit");
        BattleUnitAnimator unitAnimator = owner.AddComponent<BattleUnitAnimator>();
        vfxPrefab = new GameObject("DeathVfx");

        SetPrivateField(unitAnimator, "deathVfx", new BattleVfxEntry
        {
            prefab = vfxPrefab,
            renderMode = BattleVfxRenderMode.DirectWorldRenderer
        });

        unitAnimator.PlayDead();
        unitAnimator.PlayDead();

        Assert.That(owner.transform.childCount, Is.EqualTo(1));
        Assert.That(owner.transform.GetChild(0).name, Is.EqualTo("DeathVfx(Clone)"));
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

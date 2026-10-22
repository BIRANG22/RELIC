using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class BattleUnitAnimatorPhaseTransitionVfxTests
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
    public void PlayArabellaPhaseTransition_WhenCalledMoreThanOnce_SpawnsVfxOnlyOnce()
    {
        owner = new GameObject("Arabella");
        BattleUnitAnimator unitAnimator = owner.AddComponent<BattleUnitAnimator>();
        vfxPrefab = new GameObject("PhaseTransitionVfx");

        SetPrivateField(unitAnimator, "phaseTransitionVfx", new BattleVfxEntry
        {
            prefab = vfxPrefab,
            renderMode = BattleVfxRenderMode.DirectWorldRenderer
        });

        unitAnimator.PlayArabellaPhaseTransition();
        unitAnimator.PlayArabellaPhaseTransition();

        Assert.That(owner.transform.childCount, Is.EqualTo(1));
        Assert.That(owner.transform.GetChild(0).name, Is.EqualTo("PhaseTransitionVfx(Clone)"));
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

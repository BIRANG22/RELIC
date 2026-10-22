using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public class BattleUnitAnimatorStagedMonsterPresentationTests
{
    private GameObject owner;
    private GameObject prepareVfxPrefab;
    private GameObject actionVfxPrefab;

    [TearDown]
    public void TearDown()
    {
        if (owner != null)
            Object.DestroyImmediate(owner);

        if (prepareVfxPrefab != null)
            Object.DestroyImmediate(prepareVfxPrefab);

        if (actionVfxPrefab != null)
            Object.DestroyImmediate(actionVfxPrefab);
    }

    [Test]
    public void PlayMonsterSkillPrepare_SpawnsOnlyPrepareVfxAndReturnsDuration()
    {
        BattleUnitAnimator animator = CreateAnimatorWithStagedPresentation();
        MonsterReservedCommand command = CreateCommand();

        float duration = animator.PlayMonsterSkillPrepare(command);

        Assert.That(duration, Is.EqualTo(0.4f).Within(0.001f));
        Assert.That(owner.transform.Find("PrepareVfx(Clone)"), Is.Not.Null);
        Assert.That(owner.transform.Find("ActionVfx(Clone)"), Is.Null);
    }

    [Test]
    public void PlayMonsterSkillActionPhase_SpawnsOnlyActionVfx()
    {
        BattleUnitAnimator animator = CreateAnimatorWithStagedPresentation();
        MonsterReservedCommand command = CreateCommand();

        animator.PlayMonsterSkillActionPhase(command);

        Assert.That(owner.transform.Find("PrepareVfx(Clone)"), Is.Null);
        Assert.That(owner.transform.Find("ActionVfx(Clone)"), Is.Not.Null);
    }

    private BattleUnitAnimator CreateAnimatorWithStagedPresentation()
    {
        owner = new GameObject("Monster");
        BattleUnitAnimator animator = owner.AddComponent<BattleUnitAnimator>();
        prepareVfxPrefab = new GameObject("PrepareVfx");
        actionVfxPrefab = new GameObject("ActionVfx");

        BattleUnitActionPresentation presentation = new BattleUnitActionPresentation
        {
            monsterSkillId = "S_Monster_39",
            prepareStateName = "Takeoff",
            prepareDuration = 0.4f,
            prepareVfx = CreateDirectVfx(prepareVfxPrefab),
            stateName = "Landing",
            vfx = CreateDirectVfx(actionVfxPrefab)
        };

        BattleUnitActionPresentation[] presentations =
            BattleUnitActionPresentation.CreateArray(MonsterMasterData.PossibleSkillSlotCount);
        presentations[0] = presentation;
        SetPrivateField(animator, "monsterActionPresentations", presentations);
        return animator;
    }

    private static MonsterReservedCommand CreateCommand()
    {
        return new MonsterReservedCommand(null, new MonsterSkillData
        {
            SkillId = "S_Monster_39",
            TimelineNotation = TimelineActionType.Attack
        });
    }

    private static BattleVfxEntry CreateDirectVfx(GameObject prefab)
    {
        return new BattleVfxEntry
        {
            prefab = prefab,
            renderMode = BattleVfxRenderMode.DirectWorldRenderer
        };
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

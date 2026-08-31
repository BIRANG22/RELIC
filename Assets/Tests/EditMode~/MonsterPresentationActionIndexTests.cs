using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public class MonsterPresentationActionIndexTests
{
    private GameObject dataManagerObject;

    [TearDown]
    public void TearDown()
    {
        if (dataManagerObject != null)
            Object.DestroyImmediate(dataManagerObject);
    }

    [Test]
    public void PresentationActionIndex_UsesContinuousNonMoveSlots()
    {
        CreateDataManagerWithMonsterSkills(
            new MonsterSkillData
            {
                SkillId = "S_Test_Move",
                TimelineNotation = TimelineActionType.Move
            },
            new MonsterSkillData
            {
                SkillId = "S_Test_Attack",
                TimelineNotation = TimelineActionType.Attack
            },
            new MonsterSkillData
            {
                SkillId = "S_Test_Buff",
                TimelineNotation = TimelineActionType.Buff
            },
            new MonsterSkillData
            {
                SkillId = "S_Test_Debuff",
                TimelineNotation = TimelineActionType.Debuff
            });

        MonsterRuntimeData runtime = new(
            "Runtime_Presentation_Index",
            new MonsterMasterData
            {
                MonsterId = "Mon_Test",
                HP = 10,
                PossSkillId01 = "S_Test_Move",
                PossSkillId02 = "S_Test_Attack",
                PossSkillId03 = "S_Test_Buff",
                PossSkillId04 = "S_Test_Debuff"
            });

        Assert.That(runtime.GetPresentationActionIndexForSkill("S_Test_Move"), Is.EqualTo(0));
        Assert.That(runtime.GetPresentationActionIndexForSkill("S_Test_Attack"), Is.EqualTo(1));
        Assert.That(runtime.GetPresentationActionIndexForSkill("S_Test_Buff"), Is.EqualTo(2));
        Assert.That(runtime.GetPresentationActionIndexForSkill("S_Test_Debuff"), Is.EqualTo(3));
    }

    [Test]
    public void PresentationActionIndex_ExcludesEffectMoveSlots()
    {
        CreateDataManagerWithMonsterSkills(
            new MonsterSkillData
            {
                SkillId = "S_Test_Effect_Move",
                TimelineNotation = TimelineActionType.Buff,
                EffectIds = "E_Move"
            },
            new MonsterSkillData
            {
                SkillId = "S_Test_Buff",
                TimelineNotation = TimelineActionType.Buff
            });

        MonsterRuntimeData runtime = new(
            "Runtime_Effect_Move_Presentation_Index",
            new MonsterMasterData
            {
                MonsterId = "Mon_Test",
                HP = 10,
                PossSkillId01 = "S_Test_Effect_Move",
                PossSkillId02 = "S_Test_Buff"
            });

        Assert.That(runtime.GetPresentationActionIndexForSkill("S_Test_Effect_Move"), Is.EqualTo(0));
        Assert.That(runtime.GetPresentationActionIndexForSkill("S_Test_Buff"), Is.EqualTo(1));
    }

    private void CreateDataManagerWithMonsterSkills(params MonsterSkillData[] skills)
    {
        dataManagerObject = new GameObject("DataManager_MonsterPresentationActionIndex");
        DataManager dataManager = dataManagerObject.AddComponent<DataManager>();
        dataManager.MonsterSkillDatabase.Initialize(skills);
    }
}

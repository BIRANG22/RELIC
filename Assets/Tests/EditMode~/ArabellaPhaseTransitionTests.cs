using NUnit.Framework;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;
using System.Collections.Generic;

public class ArabellaPhaseTransitionTests
{
    [TestCase("Mon_12_1", true)]
    [TestCase("Mon_12_2", true)]
    [TestCase("Mon_12", false)]
    [TestCase("Mon_11", false)]
    public void IsArabellaMonsterId_RecognizesBothCurrentPhaseIds(string monsterId, bool expected)
    {
        Assert.That(ArabellaPhaseTransition.IsArabellaMonsterId(monsterId), Is.EqualTo(expected));
    }

    [Test]
    public void TryCreatePhaseTwo_DeadPhaseOne_PreservesIdentityAndAppliesPhaseTwoData()
    {
        MonsterRuntimeData phaseOne = new MonsterRuntimeData(
            "runtime-arabella",
            CreateMaster("Mon_12_1", 1, 105, 0));
        phaseOne.Direction = BattleDirection.Right;
        phaseOne.CurrentHP = 0;

        bool transitioned = ArabellaPhaseTransition.TryCreatePhaseTwo(
            phaseOne,
            CreateMaster("Mon_12_2", 2, 80, 95),
            out MonsterRuntimeData phaseTwo);

        Assert.That(transitioned, Is.True);
        Assert.That(phaseTwo.RuntimeId, Is.EqualTo("runtime-arabella"));
        Assert.That(phaseTwo.MonsterId, Is.EqualTo("Mon_12_2"));
        Assert.That(phaseTwo.Phase, Is.EqualTo(2));
        Assert.That(phaseTwo.MaxHP, Is.EqualTo(80));
        Assert.That(phaseTwo.CurrentHP, Is.EqualTo(80));
        Assert.That(phaseTwo.CurrentShield, Is.EqualTo(95));
        Assert.That(phaseTwo.Direction, Is.EqualTo(BattleDirection.Right));
        Assert.That(phaseTwo.IsDeathHandled, Is.False);
        Assert.That(phaseTwo.StatusEffects.Exists(effect => effect.EffectId == "E_Entanglement"), Is.True);
    }

    [Test]
    public void TryCreatePhaseTwo_DeadPhaseTwo_DoesNotTransitionAgain()
    {
        MonsterRuntimeData phaseTwo = new MonsterRuntimeData(
            "runtime-arabella",
            CreateMaster("Mon_12_2", 2, 80, 95));
        phaseTwo.CurrentHP = 0;

        bool transitioned = ArabellaPhaseTransition.TryCreatePhaseTwo(
            phaseTwo,
            CreateMaster("Mon_12_2", 2, 80, 95),
            out MonsterRuntimeData next);

        Assert.That(transitioned, Is.False);
        Assert.That(next, Is.Null);
    }

    [TestCase("Idle", "Idle_b")]
    [TestCase("Move", "Move_b")]
    [TestCase("Guard", "Guard_b")]
    [TestCase("Hit", "Hit_b")]
    [TestCase("Dead", "Dead_b")]
    [TestCase("AttackAction3", "AttackAction3_b")]
    public void ResolvePhaseTwoAnimationState_UsesAwakenedState(string original, string expected)
    {
        Assert.That(
            ArabellaPhaseTransition.ResolvePhaseTwoAnimationState(original),
            Is.EqualTo(expected));
    }

    [Test]
    public void Initialize_WhenRuntimeChanges_RebindsExistingHud()
    {
        GameObject monsterObject = new GameObject("Arabella");
        GameObject hudObject = new GameObject("HUD");

        try
        {
            MonsterUnit monster = monsterObject.AddComponent<MonsterUnit>();
            HUDSlot hud = hudObject.AddComponent<HUDSlot>();
            MonsterRuntimeData phaseOne = new MonsterRuntimeData(
                "runtime-arabella",
                CreateMaster("Mon_12_1", 1, 105, 0));
            MonsterRuntimeData phaseTwo = new MonsterRuntimeData(
                "runtime-arabella",
                CreateMaster("Mon_12_2", 2, 80, 95));

            monster.Initialize(phaseOne);
            monster.BindHUD(hud);
            monster.Initialize(phaseTwo);

            Assert.That(hud.BoundMonsterRuntime, Is.SameAs(phaseTwo));
        }
        finally
        {
            Object.DestroyImmediate(hudObject);
            Object.DestroyImmediate(monsterObject);
        }
    }

    [Test]
    public void FilterOwnedSkills_PhaseTwo_RemovesSkillsMissingFromMasterData()
    {
        MonsterMasterData master = CreateMaster("Mon_12_2", 2, 80, 0);
        master.PossSkillId01 = "S_Monster_33";
        master.PossSkillId02 = "S_Monster_34";
        master.PossSkillId03 = "S_Monster_35";
        master.PossSkillId04 = "S_Monster_39";
        master.PossSkillId05 = "S_Monster_40";
        MonsterRuntimeData runtime = new MonsterRuntimeData("runtime-arabella", master);

        List<string> filtered = ArabellaPhaseTransition.FilterOwnedSkills(
            runtime,
            new[]
            {
                "S_Monster_33",
                "S_Monster_36",
                "S_Monster_37",
                "S_Monster_39",
                "S_Monster_40"
            });

        Assert.That(filtered, Is.EqualTo(new[]
        {
            "S_Monster_33",
            "S_Monster_39",
            "S_Monster_40"
        }));
    }

    private static MonsterMasterData CreateMaster(
        string monsterId,
        int phase,
        int hp,
        int armor)
    {
        return new MonsterMasterData
        {
            MonsterId = monsterId,
            Phase = phase,
            Name = monsterId,
            Grade = "Boss",
            HP = hp,
            Armor = armor
        };
    }
}

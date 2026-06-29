using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;

public class AnimationVfxLoadoutCleanupTests
{
    [Test]
    public void LoadoutWrapperTypes_AreRemoved()
    {
        Assert.That(FindType("Relic.Gameplay.Data.CharacterSkillLoadout"), Is.Null);
        Assert.That(FindType("Relic.Gameplay.Data.CharacterRuneLoadout"), Is.Null);
        Assert.That(FindType("Relic.Gameplay.Data.MonsterSkillLoadoutData"), Is.Null);
    }

    [Test]
    public void CharacterEquipmentManager_WritesDirectEquipmentFields()
    {
        CharacterEquipmentManager manager = new();

        manager.EquipPassive("C_Test", "Passive_01");
        manager.EquipUnique("C_Test", "Unique_01");
        manager.EquipAbility("C_Test", "Ability_01");
        manager.EquipFreeSkill("C_Test", 1, "Free_02");
        manager.EquipRune("C_Test", 4, "Rune_05");
        manager.EquipFragment("C_Test", 3, "Fragment_04");

        CharacterEquipmentData equipment = manager.GetOrCreate("C_Test");

        Assert.That(equipment.PassiveSkillId, Is.EqualTo("Passive_01"));
        Assert.That(equipment.UniqueSkillId, Is.EqualTo("Unique_01"));
        Assert.That(equipment.AbilitySkillId, Is.EqualTo("Ability_01"));
        Assert.That(equipment.FreeSkillIds, Has.Length.EqualTo(2));
        Assert.That(equipment.FreeSkillIds[1], Is.EqualTo("Free_02"));
        Assert.That(equipment.RuneIds, Has.Length.EqualTo(5));
        Assert.That(equipment.RuneIds[4], Is.EqualTo("Rune_05"));
        Assert.That(equipment.FragmentIds, Has.Length.EqualTo(4));
        Assert.That(equipment.FragmentIds[3], Is.EqualTo("Fragment_04"));
    }

    [Test]
    public void BattleUnitActionPresentation_UsesSingleFrameFieldsOnly()
    {
        Type presentationType = typeof(BattleUnitActionPresentation);

        Assert.That(presentationType.GetField("stateName"), Is.Not.Null);
        Assert.That(presentationType.GetField("vfx"), Is.Not.Null);
        Assert.That(presentationType.GetField("readyStateName"), Is.Null);
        Assert.That(presentationType.GetField("readyVfx"), Is.Null);
        Assert.That(presentationType.GetField("actionStateName"), Is.Null);
        Assert.That(presentationType.GetField("actionVfx"), Is.Null);
    }

    [Test]
    public void BattleActionRunner_DoesNotKeepReadyDelayStage()
    {
        FieldInfo readyDelayField = typeof(BattleActionRunner).GetField(
            "ReadyDelay",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.That(readyDelayField, Is.Null);
    }

    [Test]
    public void BattleUnitAnimator_GroupsPlayerSkillPresentationsAndRemovesLegacyAttackFields()
    {
        Type animatorType = typeof(BattleUnitAnimator);
        Type playerPresentationsType = typeof(BattleUnitPlayerSkillPresentations);

        Assert.That(animatorType.GetField("playerSkillPresentations", BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
        Assert.That(playerPresentationsType.GetField("power"), Is.Not.Null);
        Assert.That(playerPresentationsType.GetField("attack1"), Is.Not.Null);
        Assert.That(playerPresentationsType.GetField("attack2"), Is.Not.Null);
        Assert.That(playerPresentationsType.GetField("attack3"), Is.Not.Null);
        Assert.That(playerPresentationsType.GetField("skill"), Is.Not.Null);

        Assert.That(animatorType.GetField("playerPowerPresentation", BindingFlags.Instance | BindingFlags.NonPublic), Is.Null);
        Assert.That(animatorType.GetField("playerSkillPresentation", BindingFlags.Instance | BindingFlags.NonPublic), Is.Null);
        Assert.That(animatorType.GetField("attackReady1StateName", BindingFlags.Instance | BindingFlags.NonPublic), Is.Null);
        Assert.That(animatorType.GetField("attackAction1StateName", BindingFlags.Instance | BindingFlags.NonPublic), Is.Null);
        Assert.That(animatorType.GetField("attackVfx1", BindingFlags.Instance | BindingFlags.NonPublic), Is.Null);
    }

    [Test]
    public void BattleUnitAnimator_UsesEffectSpecificStatusVfxSet()
    {
        Type animatorType = typeof(BattleUnitAnimator);
        Type statusVfxType = typeof(BattleStatusVfxSet);

        Assert.That(animatorType.GetField("statusVfx", BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
        Assert.That(statusVfxType.GetField("powerVfx"), Is.Not.Null);
        Assert.That(statusVfxType.GetField("weakenVfx"), Is.Not.Null);

        Assert.That(animatorType.GetField("buffVfx", BindingFlags.Instance | BindingFlags.NonPublic), Is.Null);
        Assert.That(animatorType.GetField("debuffVfx", BindingFlags.Instance | BindingFlags.NonPublic), Is.Null);
    }

    [Test]
    public void MonsterMasterData_NormalizesPossibleSkillSlotsAndPreservesActionIndex()
    {
        MonsterMasterData master = new()
        {
            MonsterId = "M_Slots",
            HP = 10,
            PossSkillId01 = "S_Monster_A",
            PossSkillId02 = "0",
            PossSkillId03 = "",
            PossSkillId04 = null,
            PossSkillId05 = "   ",
            PossSkillId10 = "S_Monster_J"
        };

        Assert.That(master.GetPossibleSkillIds(), Is.EqualTo(new[] { "S_Monster_A", "S_Monster_J" }));
        Assert.That(
            master.GetPossibleSkillIdSlots(),
            Is.EqualTo(new[] { "S_Monster_A", "", "", "", "", "", "", "", "", "S_Monster_J" }));
        Assert.That(master.GetPossibleSkillIdAtActionIndex(-1), Is.EqualTo(""));
        Assert.That(master.GetPossibleSkillIdAtActionIndex(0), Is.EqualTo(""));
        Assert.That(master.GetPossibleSkillIdAtActionIndex(1), Is.EqualTo("S_Monster_A"));
        Assert.That(master.GetPossibleSkillIdAtActionIndex(10), Is.EqualTo("S_Monster_J"));
        Assert.That(master.GetPossibleSkillIdAtActionIndex(11), Is.EqualTo(""));
        Assert.That(master.GetActionIndexForSkill("S_Monster_J"), Is.EqualTo(10));
        Assert.That(master.GetActionIndexForSkill("Missing"), Is.EqualTo(0));
    }

    [Test]
    public void MonsterRuntimeData_CopiesPossibleSkillSlotsFromMaster()
    {
        MonsterMasterData master = new()
        {
            MonsterId = "M_Runtime_Slots",
            Name = "Runtime Slots",
            HP = 10,
            PossSkillId01 = "S_Monster_A",
            PossSkillId05 = "S_Monster_E",
            PossSkillId10 = "0"
        };

        MonsterRuntimeData runtime = new("Runtime_01", master);

        Assert.That(runtime.PossSkillIds, Is.EqualTo(new[] { "S_Monster_A", "S_Monster_E" }));
        Assert.That(runtime.GetActionIndexForSkill("S_Monster_E"), Is.EqualTo(5));
    }

    [Test]
    public void MonsterRuntimeData_NullMasterCreatesEmptyPossibleSkillSlots()
    {
        MonsterRuntimeData runtime = new("Runtime_Null", null);

        Assert.That(runtime.RuntimeId, Is.EqualTo("Runtime_Null"));
        Assert.That(runtime.PossibleSkillIdsByActionIndex, Has.Length.EqualTo(MonsterMasterData.PossibleSkillSlotCount));
        Assert.That(runtime.PossibleSkillIdsByActionIndex, Is.All.EqualTo(""));
        Assert.That(runtime.PossSkillIds, Is.Empty);
        Assert.That(runtime.GetActionIndexForSkill("AnySkill"), Is.EqualTo(0));
    }

    [Test]
    public void MonsterReservedCommand_ResolvesActionIndexFromRuntimeSkillSlots()
    {
        MonsterMasterData master = new()
        {
            MonsterId = "M_Command_Slots",
            Name = "Command Slots",
            HP = 10,
            PossSkillId04 = "S_Monster_Action04"
        };
        MonsterRuntimeData runtime = new("Runtime_Command", master);
        MonsterSkillData skill = new() { SkillId = "S_Monster_Action04" };

        MonsterReservedCommand command = new(runtime, skill);

        Assert.That(command.ActionIndex, Is.EqualTo(4));
    }

    [Test]
    public void MonsterReservedCommand_SetActionIndexClampsToPossibleSkillSlots()
    {
        MonsterRuntimeData runtime = new("Runtime_Command", new MonsterMasterData());
        MonsterReservedCommand command = new(runtime, new MonsterSkillData());

        command.SetActionIndex(-1);
        Assert.That(command.ActionIndex, Is.EqualTo(0));

        command.SetActionIndex(MonsterMasterData.PossibleSkillSlotCount + 1);
        Assert.That(command.ActionIndex, Is.EqualTo(MonsterMasterData.PossibleSkillSlotCount));
    }

    [Test]
    public void BattleUnitAnimator_PlayerPowerActionSpawnsPowerVfx()
    {
        GameObject owner = new("AnimatorOwner");
        GameObject powerPrefab = new("PowerVfx");

        try
        {
            BattleUnitAnimator animator = owner.AddComponent<BattleUnitAnimator>();

            SetPrivateField(animator, "playerSkillPresentations", new BattleUnitPlayerSkillPresentations
            {
                power = new BattleUnitActionPresentation
                {
                    stateName = "",
                    vfx = new BattleVfxEntry { prefab = powerPrefab, flipType = VfxFlipType.None }
                }
            });

            animator.PlaySkillAction(new SkillMasterData { SkillId = "S_Power", SkillType = SkillType.Power });

            Assert.That(owner.transform.Find("PowerVfx(Clone)"), Is.Not.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(powerPrefab);
            UnityEngine.Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void BattleUnitAnimator_PlayerPowerReadyDoesNotSpawnPresentationVfx()
    {
        GameObject owner = new("AnimatorOwner");
        GameObject powerPrefab = new("PowerReadyVfx");

        try
        {
            BattleUnitAnimator animator = owner.AddComponent<BattleUnitAnimator>();

            SetPrivateField(animator, "playerSkillPresentations", new BattleUnitPlayerSkillPresentations
            {
                power = new BattleUnitActionPresentation
                {
                    stateName = "",
                    vfx = new BattleVfxEntry { prefab = powerPrefab, flipType = VfxFlipType.None }
                }
            });

            animator.PlaySkillReady(new SkillMasterData { SkillId = "S_Power", SkillType = SkillType.Power });

            Assert.That(owner.transform.Find("PowerReadyVfx(Clone)"), Is.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(powerPrefab);
            UnityEngine.Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void BattleUnitAnimator_PlayerAttackActionSpawnsGroupedAttackVfx()
    {
        GameObject owner = new("AnimatorOwner");
        GameObject attackPrefab = new("PlayerAttack1Vfx");

        try
        {
            BattleUnitAnimator animator = owner.AddComponent<BattleUnitAnimator>();

            SetPrivateField(animator, "playerSkillPresentations", new BattleUnitPlayerSkillPresentations
            {
                attack1 = new BattleUnitActionPresentation
                {
                    stateName = "",
                    vfx = new BattleVfxEntry { prefab = attackPrefab, flipType = VfxFlipType.None }
                }
            });

            animator.PlayAttackAction(1);

            Assert.That(owner.transform.Find("PlayerAttack1Vfx(Clone)"), Is.Not.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(attackPrefab);
            UnityEngine.Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void BattleUnitAnimator_MonsterCommandActionSpawnsMatchingActionVfx()
    {
        GameObject owner = new("MonsterAnimatorOwner");
        GameObject action4Prefab = new("MonsterAction4Vfx");

        try
        {
            BattleUnitAnimator animator = owner.AddComponent<BattleUnitAnimator>();

            BattleUnitActionPresentation[] slots = BattleUnitActionPresentation.CreateArray(10);
            slots[3].stateName = "";
            slots[3].vfx = new BattleVfxEntry { prefab = action4Prefab, flipType = VfxFlipType.None };
            SetPrivateField(animator, "monsterActionPresentations", slots);

            MonsterMasterData master = new()
            {
                MonsterId = "M_Action4",
                HP = 10,
                PossSkillId04 = "S_Monster_Action4"
            };
            MonsterRuntimeData runtime = new("Runtime_Action4", master);
            MonsterReservedCommand command = new(runtime, new MonsterSkillData { SkillId = "S_Monster_Action4" });

            animator.PlayMonsterSkillAction(command);

            Assert.That(owner.transform.Find("MonsterAction4Vfx(Clone)"), Is.Not.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(action4Prefab);
            UnityEngine.Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void BattleUnitAnimator_MonsterCommandReadyDoesNotSpawnPresentationVfx()
    {
        GameObject owner = new("MonsterAnimatorOwner");
        GameObject action4Prefab = new("MonsterAction4ReadyVfx");

        try
        {
            BattleUnitAnimator animator = owner.AddComponent<BattleUnitAnimator>();

            BattleUnitActionPresentation[] slots = BattleUnitActionPresentation.CreateArray(10);
            slots[3].stateName = "";
            slots[3].vfx = new BattleVfxEntry { prefab = action4Prefab, flipType = VfxFlipType.None };
            SetPrivateField(animator, "monsterActionPresentations", slots);

            MonsterMasterData master = new()
            {
                MonsterId = "M_Action4",
                HP = 10,
                PossSkillId04 = "S_Monster_Action4"
            };
            MonsterRuntimeData runtime = new("Runtime_Action4", master);
            MonsterReservedCommand command = new(runtime, new MonsterSkillData { SkillId = "S_Monster_Action4" });

            animator.PlayMonsterSkillReady(command);

            Assert.That(owner.transform.Find("MonsterAction4ReadyVfx(Clone)"), Is.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(action4Prefab);
            UnityEngine.Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void BattleUnitAnimator_MonsterCommandActionWithUnmappedActionIndexDoesNotSpawnSlot1Vfx()
    {
        GameObject owner = new("MonsterAnimatorOwner");
        GameObject action1Prefab = new("MonsterAction1Vfx");

        try
        {
            BattleUnitAnimator animator = owner.AddComponent<BattleUnitAnimator>();

            BattleUnitActionPresentation[] slots = BattleUnitActionPresentation.CreateArray(10);
            slots[0].stateName = "";
            slots[0].vfx = new BattleVfxEntry { prefab = action1Prefab, flipType = VfxFlipType.None };
            SetPrivateField(animator, "monsterActionPresentations", slots);

            MonsterMasterData master = new()
            {
                MonsterId = "M_Unmapped",
                HP = 10
            };
            MonsterRuntimeData runtime = new("Runtime_Unmapped", master);
            MonsterReservedCommand command = new(
                runtime,
                new MonsterSkillData
                {
                    SkillId = "S_Monster_Unmapped",
                    TimelineNotation = TimelineActionType.Attack
                });

            Assert.That(command.ActionIndex, Is.EqualTo(0));

            animator.PlayMonsterSkillAction(command);

            Assert.That(owner.transform.Find("MonsterAction1Vfx(Clone)"), Is.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(action1Prefab);
            UnityEngine.Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void BattleUnitAnimator_MonsterCommandActionDoesNotUsePlayerAttackVfx()
    {
        GameObject owner = new("MonsterAnimatorOwner");
        GameObject playerAttackPrefab = new("PlayerOnlyAttackVfx");

        try
        {
            BattleUnitAnimator animator = owner.AddComponent<BattleUnitAnimator>();
            SetPrivateField(animator, "playerSkillPresentations", new BattleUnitPlayerSkillPresentations
            {
                attack1 = new BattleUnitActionPresentation
                {
                    stateName = "",
                    vfx = new BattleVfxEntry { prefab = playerAttackPrefab, flipType = VfxFlipType.None }
                }
            });

            MonsterRuntimeData runtime = new("Runtime_Unmapped", new MonsterMasterData { HP = 10 });
            MonsterReservedCommand command = new(
                runtime,
                new MonsterSkillData
                {
                    SkillId = "S_Monster_Unmapped",
                    TimelineNotation = TimelineActionType.Attack
                });

            animator.PlayMonsterSkillAction(command);

            Assert.That(owner.transform.Find("PlayerOnlyAttackVfx(Clone)"), Is.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(playerAttackPrefab);
            UnityEngine.Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void BattleUnitAnimator_PlayBuffAndDebuffDoNotSpawnStatusVfx()
    {
        GameObject owner = new("StatusUseOwner");
        GameObject buffPrefab = new("UseBuffVfx");
        GameObject debuffPrefab = new("UseDebuffVfx");

        try
        {
            BattleUnitAnimator animator = owner.AddComponent<BattleUnitAnimator>();
            SetPrivateField(animator, "statusVfx", new BattleStatusVfxSet
            {
                powerVfx = new BattleVfxEntry { prefab = buffPrefab, flipType = VfxFlipType.None },
                weakenVfx = new BattleVfxEntry { prefab = debuffPrefab, flipType = VfxFlipType.None }
            });

            animator.PlayBuff();
            animator.PlayDebuff();

            Assert.That(owner.transform.Find("UseBuffVfx(Clone)"), Is.Null);
            Assert.That(owner.transform.Find("UseDebuffVfx(Clone)"), Is.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(buffPrefab);
            UnityEngine.Object.DestroyImmediate(debuffPrefab);
            UnityEngine.Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void AddStatusToPlayer_SpawnsBuffVfxOnTarget()
    {
        GameObject owner = new("PlayerStatusTarget");
        GameObject buffPrefab = new("BuffStatusVfx");

        try
        {
            BattleCharacter character = owner.AddComponent<BattleCharacter>();
            BattleUnitAnimator animator = owner.AddComponent<BattleUnitAnimator>();
            SetPrivateField(animator, "statusVfx", new BattleStatusVfxSet
            {
                powerVfx = new BattleVfxEntry { prefab = buffPrefab, flipType = VfxFlipType.None }
            });

            character.Initialize(new CharacterRuntimeData
            {
                CharacterId = "C_Status",
                MaxHP = 10,
                CurrentHP = 10
            });

            BattleEffectUtility.AddStatusToPlayer(character, "E_Power", 1, 1);

            Assert.That(owner.transform.Find("BuffStatusVfx(Clone)"), Is.Not.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(buffPrefab);
            UnityEngine.Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void AddStatusToPlayer_DoesNotSpawnVfxWhenStatusListIsMissing()
    {
        GameObject owner = new("PlayerMissingStatusListTarget");
        GameObject buffPrefab = new("MissingListBuffStatusVfx");

        try
        {
            BattleCharacter character = owner.AddComponent<BattleCharacter>();
            BattleUnitAnimator animator = owner.AddComponent<BattleUnitAnimator>();
            SetPrivateField(animator, "statusVfx", new BattleStatusVfxSet
            {
                powerVfx = new BattleVfxEntry { prefab = buffPrefab, flipType = VfxFlipType.None }
            });

            character.Initialize(new CharacterRuntimeData
            {
                CharacterId = "C_Status_NullList",
                MaxHP = 10,
                CurrentHP = 10,
                StatusEffects = null
            });

            BattleEffectUtility.AddStatusToPlayer(character, "E_Power", 1, 1);

            Assert.That(owner.transform.Find("MissingListBuffStatusVfx(Clone)"), Is.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(buffPrefab);
            UnityEngine.Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void AddStatusToMonster_SpawnsDebuffVfxOnTarget()
    {
        GameObject owner = new("MonsterStatusTarget");
        GameObject debuffPrefab = new("DebuffStatusVfx");

        try
        {
            MonsterUnit monster = owner.AddComponent<MonsterUnit>();
            BattleUnitAnimator animator = owner.AddComponent<BattleUnitAnimator>();
            SetPrivateField(animator, "statusVfx", new BattleStatusVfxSet
            {
                weakenVfx = new BattleVfxEntry { prefab = debuffPrefab, flipType = VfxFlipType.None }
            });

            MonsterRuntimeData runtime = new(
                "Runtime_Status",
                new MonsterMasterData
                {
                    MonsterId = "M_Status",
                    Name = "StatusMonster",
                    HP = 10
                });

            monster.Initialize(runtime);

            BattleEffectUtility.AddStatusToMonster(monster, "E_Weaken", 1, 1);

            Assert.That(owner.transform.Find("DebuffStatusVfx(Clone)"), Is.Not.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(debuffPrefab);
            UnityEngine.Object.DestroyImmediate(owner);
        }
    }

    private static Type FindType(string fullName)
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .Select(assembly => assembly.GetType(fullName))
            .FirstOrDefault(type => type != null);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(target, value);
    }
}

using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public sealed class ReusableEquipmentEffectTests
{
    private GameObject dataManagerObject;

    [SetUp]
    public void SetUp()
    {
        if (DataManager.Instance == null)
        {
            dataManagerObject = new GameObject("DataManager_Test");
            dataManagerObject.AddComponent<DataManager>();
        }

        DataManager.Instance.RelicDatabase.Initialize(System.Array.Empty<RelicData>());
        DataManager.Instance.RuneDatabase.Initialize(System.Array.Empty<RuneData>());
        DataManager.Instance.CharacterDatabase.Initialize(System.Array.Empty<CharacterMasterData>());
    }

    [TearDown]
    public void TearDown()
    {
        if (dataManagerObject != null)
            Object.DestroyImmediate(dataManagerObject);
    }

    [Test]
    public void ReusableStatEffects_ModifyBattleStatsFromEquippedRelicsAndRunes()
    {
        DataManager.Instance.RelicDatabase.Initialize(new[]
        {
            CreateRelic(
                "Relic_Stats",
                Entry("E_Max_HP", 5),
                Entry("E_Max_Cost", 2),
                Entry("E_Cost_Recovery_Delta", 1))
        });
        DataManager.Instance.RuneDatabase.Initialize(new[]
        {
            CreateRune("Rune_Move", Entry("E_Move_Value", 8))
        });

        CharacterRuntimeData runtime = CreateRuntime();
        runtime.EquippedRelicIds[1] = "Relic_Stats";
        runtime.EquippedRuneIds[0] = "Rune_Move";

        CharacterMasterData master = new()
        {
            CharacterId = runtime.CharacterId,
            MaxHP = 40,
            MaxCost = 3,
            CostRecovery = 2
        };

        Assert.That(BattleEquipmentEffectService.GetEffectiveMaxHP(runtime, master), Is.EqualTo(45));
        Assert.That(BattleEquipmentEffectService.GetEffectiveMaxCost(runtime, master), Is.EqualTo(5));
        Assert.That(BattleEquipmentEffectService.GetEffectiveCostRecovery(runtime, master), Is.EqualTo(3));
        Assert.That(BattleEquipmentEffectService.GetEffectiveMoveValue(runtime, master), Is.EqualTo(8));
    }

    [Test]
    public void BattleStartEffects_PreservesRunMaxHpBonusWithoutAccumulatingEquipmentBonus()
    {
        DataManager.Instance.RelicDatabase.Initialize(new[]
        {
            CreateRelic("Relic_Stats", Entry("E_Max_HP", 5))
        });

        CharacterRuntimeData runtime = CreateRuntime();
        runtime.RunMaxHPBonus = 8;
        runtime.MaxHP = 48;
        runtime.CurrentHP = 48;
        runtime.EquippedRelicIds[1] = "Relic_Stats";

        CharacterMasterData master = new()
        {
            CharacterId = runtime.CharacterId,
            MaxHP = 40,
            MaxCost = 3,
            CostRecovery = 2
        };

        BattleEquipmentEffectService.ApplyBattleStartEffects(runtime, master);

        Assert.Multiple(() =>
        {
            Assert.That(runtime.MaxHP, Is.EqualTo(53));
            Assert.That(runtime.CurrentHP, Is.EqualTo(53));
        });

        BattleEquipmentEffectService.ApplyBattleStartEffects(runtime, master);

        Assert.Multiple(() =>
        {
            Assert.That(runtime.MaxHP, Is.EqualTo(53));
            Assert.That(runtime.CurrentHP, Is.EqualTo(53));
        });
    }

    [Test]
    public void BattleStartEffects_PreservesRunMaxCostBonusWithoutAccumulatingEquipmentBonus()
    {
        DataManager.Instance.RelicDatabase.Initialize(new[]
        {
            CreateRelic("Relic_Stats", Entry("E_Max_Cost", 2))
        });

        CharacterRuntimeData runtime = CreateRuntime();
        runtime.RunMaxCostBonus = 2;
        runtime.MaxCost = 5;
        runtime.CurrentCost = 5;
        runtime.EquippedRelicIds[1] = "Relic_Stats";

        CharacterMasterData master = new()
        {
            CharacterId = runtime.CharacterId,
            MaxHP = 40,
            MaxCost = 3,
            CostRecovery = 2
        };

        BattleEquipmentEffectService.ApplyBattleStartEffects(runtime, master);

        Assert.That(runtime.MaxCost, Is.EqualTo(7));

        BattleEquipmentEffectService.ApplyBattleStartEffects(runtime, master);

        Assert.That(runtime.MaxCost, Is.EqualTo(7));
    }

    [Test]
    public void ReusableSkillModifierEffects_ModifyReservationCostValueAndCount()
    {
        DataManager.Instance.RelicDatabase.Initialize(new[]
        {
            CreateRelic(
                "Relic_AttackMods",
                Entry("E_Attack_Cost_Delta", 1),
                Entry("E_Attack_Value_Delta", 2),
                Entry("E_Attack_Count_Delta", 1),
                Entry("E_Slot5_Attack_Value_Delta", 3))
        });

        CharacterRuntimeData runtime = CreateRuntime();
        runtime.EquippedRelicIds[1] = "Relic_AttackMods";
        PlayerReservedCommand command = new(runtime, CreateSkill("S_Attack", SkillType.Attack, 3));

        BattleEquipmentEffectService.ApplyReservationCostModifiers(
            command,
            4,
            isFirstMoveCommand: false,
            isLastTimelineSlot: true);

        SkillEffectEntry strike = Entry("E_Strike", 5);

        Assert.That(command.Cost, Is.EqualTo(4));
        Assert.That(
            BattleEquipmentEffectService.ModifyPlayerEffectValue(runtime, command, strike, 5),
            Is.EqualTo(10));
        Assert.That(
            BattleEquipmentEffectService.ModifyPlayerEffectCount(runtime, command, strike, 1),
            Is.EqualTo(2));
    }

    [Test]
    public void ReusableReservationContextEffects_ModifyFirstSlotAndUniqueResourceCosts()
    {
        DataManager.Instance.RelicDatabase.Initialize(new[]
        {
            CreateRelic(
                "Relic_Context",
                Entry("E_Slot1_First_Skill_Cost_Delta", -1),
                Entry("E_UniqueResource_Min_Use_Delta", -2))
        });

        CharacterRuntimeData runtime = CreateRuntime();
        runtime.CurrentResource = 3;
        runtime.EquippedRelicIds[1] = "Relic_Context";

        PlayerReservedCommand firstCommand = new(runtime, CreateSkill("S_Attack", SkillType.Attack, 3));
        BattleEquipmentEffectService.ApplyReservationCostModifiers(
            firstCommand,
            0,
            isFirstMoveCommand: false,
            isLastTimelineSlot: false,
            isFirstSkillInSlot: true);

        PlayerReservedCommand secondCommand = new(runtime, CreateSkill("S_Attack", SkillType.Attack, 3));
        BattleEquipmentEffectService.ApplyReservationCostModifiers(
            secondCommand,
            0,
            isFirstMoveCommand: false,
            isLastTimelineSlot: false,
            isFirstSkillInSlot: false);

        PlayerReservedCommand uniqueCommand = new(
            runtime,
            CreateSkill("S_Unique_Test", SkillType.Attack, 3, ReferenceResource.UniqueResource, Category.Unique));
        BattleEquipmentEffectService.ApplyReservationCostModifiers(
            uniqueCommand,
            2,
            isFirstMoveCommand: false,
            isLastTimelineSlot: false);

        Assert.That(firstCommand.Cost, Is.EqualTo(2));
        Assert.That(secondCommand.Cost, Is.EqualTo(3));
        Assert.That(uniqueCommand.ResourceCost, Is.EqualTo(1));
    }

    [Test]
    public void ReusableRangeAndPierceEffects_ReplacesSpecificRangeAndDamageEffect()
    {
        DataManager.Instance.RelicDatabase.Initialize(new[]
        {
            CreateRelic(
                "Relic_RangePierce",
                Entry("E_Range_Delta", 2),
                Entry("E_Slot5_Attack_Pierce", 1))
        });

        CharacterRuntimeData runtime = CreateRuntime();
        runtime.EquippedRelicIds[1] = "Relic_RangePierce";
        SkillMasterData skill = CreateSkill("S_Attack", SkillType.Attack, 3);
        skill.RangeId = "Range_21";

        PlayerReservedCommand command = new(runtime, skill);
        BattleEquipmentEffectService.ApplyReservationCostModifiers(
            command,
            4,
            isFirstMoveCommand: false,
            isLastTimelineSlot: true);

        Assert.That(
            BattleEquipmentEffectService.GetEffectiveRangeId(runtime, skill),
            Is.EqualTo("Range_18"));
        Assert.That(
            BattleEquipmentEffectService.GetEffectivePlayerDamageEffectId(runtime, command, "E_Strike"),
            Is.EqualTo("E_Pierce"));
    }

    [Test]
    public void ReusableTurnStartEffects_ApplyOnlyOnConfiguredTurn()
    {
        DataManager.Instance.RelicDatabase.Initialize(new[]
        {
            CreateRelic(
                "Relic_Turn2",
                Entry("E_Turn_Start_Armor", 10, 2),
                Entry("E_Turn_Start_Charge", 1, 2),
                Entry("E_Turn_Start_Focus", 2, 2))
        });

        CharacterRuntimeData runtime = CreateRuntime();
        runtime.EquippedRelicIds[1] = "Relic_Turn2";

        BattleEquipmentEffectService.ApplyPlayerTurnStartEffects(runtime, 1);

        Assert.That(runtime.CurrentShield, Is.EqualTo(0));
        Assert.That(GetStatusStack(runtime, "E_Charge"), Is.EqualTo(0));

        BattleEquipmentEffectService.ApplyPlayerTurnStartEffects(runtime, 2);

        Assert.That(runtime.CurrentShield, Is.EqualTo(10));
        Assert.That(GetStatusStack(runtime, "E_Charge"), Is.EqualTo(1));
        Assert.That(GetStatusStack(runtime, "E_Focus"), Is.EqualTo(2));
    }

    [Test]
    public void ActiveRelicResolver_RecognizesExplicitGridEffectRelicIds()
    {
        Assert.That(
            ActiveRelicEffectResolver.ResolveTargetMode("AR_SpawnThornGridEffect"),
            Is.EqualTo(ActiveRelicTargetMode.Grid));
    }

    [Test]
    public void RuntimeData_DoesNotLeaveRuneOrRelicEffectIdsAsPlaceholderValue()
    {
        string csv = File.ReadAllText("Assets/Resources/Data/GameDataRuntime.csv");

        Assert.That(csv, Does.Not.Contain(",E_Value"));
    }

    private static CharacterRuntimeData CreateRuntime()
    {
        return new CharacterRuntimeData
        {
            CharacterId = "Char_Test",
            MaxHP = 40,
            CurrentHP = 40,
            MaxCost = 3,
            CurrentCost = 10,
            CostRecovery = 2,
            EquippedRelicIds = new string[5],
            EquippedRuneIds = new string[12],
            StatusEffects = new List<StatusEffectRuntimeData>()
        };
    }

    private static RelicData CreateRelic(string id, params SkillEffectEntry[] entries)
    {
        return new RelicData
        {
            FragmentId = id,
            Type = "Passive",
            EffectEntries = new List<SkillEffectEntry>(entries)
        };
    }

    private static RuneData CreateRune(string id, params SkillEffectEntry[] entries)
    {
        return new RuneData
        {
            RuneId = id,
            EffectEntries = new List<SkillEffectEntry>(entries)
        };
    }

    private static SkillMasterData CreateSkill(
        string id,
        SkillType skillType,
        int cost,
        ReferenceResource referenceResource = ReferenceResource.Cost,
        Category category = Category.Public)
    {
        return new SkillMasterData
        {
            SkillId = id,
            Category = category,
            SkillType = skillType,
            ReferenceResource = referenceResource,
            ResourceCostValue = cost
        };
    }

    private static SkillEffectEntry Entry(string effectId, int value, int count = 1)
    {
        return new SkillEffectEntry
        {
            EffectId = effectId,
            ValueAmount = value,
            CountAmount = count
        };
    }

    private static int GetStatusStack(CharacterRuntimeData runtime, string effectId)
    {
        if (runtime?.StatusEffects == null)
            return 0;

        for (int i = 0; i < runtime.StatusEffects.Count; i++)
        {
            StatusEffectRuntimeData status = runtime.StatusEffects[i];

            if (status != null && status.EffectId == effectId)
                return status.Stack;
        }

        return 0;
    }
}

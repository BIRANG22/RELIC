using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;

public sealed class BattleResearchEffectTests
{
    private readonly List<GameObject> createdObjects = new();

    [TearDown]
    public void TearDown()
    {
        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            if (createdObjects[i] != null)
                Object.DestroyImmediate(createdObjects[i]);
        }

        createdObjects.Clear();
        DestroyIfExists("BattleDamageTextPopupUI_Auto");
        DestroyIfExists("BattleDamageTextCanvas_Auto");
    }

    [Test]
    public void AddOrStackStatus_UsesCountRateAsRepeatCount()
    {
        List<StatusEffectRuntimeData> statuses = new();

        Assert.That(
            BattleEffectUtility.AddOrStackStatus(statuses, "E_Test", 2, 2),
            Is.True);

        Assert.That(statuses, Has.Count.EqualTo(1));
        Assert.That(statuses[0].Stack, Is.EqualTo(4));
        Assert.That(statuses[0].TurnCount, Is.EqualTo(1));

        Assert.That(
            BattleEffectUtility.AddOrStackStatus(statuses, "E_Test", 3, 2),
            Is.True);

        Assert.That(statuses[0].Stack, Is.EqualTo(10));
        Assert.That(statuses[0].TurnCount, Is.EqualTo(1));
    }

    [Test]
    public void Registry_ResolvesResearchEffectScripts()
    {
        BattleEffectRegistry registry = new();

        string[] effectIds =
        {
            "E_Move_First_Attack_Power",
            "E_Poison_Apply_Double",
            "E_Bleeding_Apply_Double",
            "E_Max_HP_Up",
            "E_Kill_Heal",
            "E_Skill_Resource_Gain_Up",
            "E_Buff_Apply_Double",
            "E_Low_HP_Power",
            "E_Move_Point_Up"
        };

        for (int i = 0; i < effectIds.Length; i++)
            Assert.That(registry.Get(effectIds[i]), Is.Not.Null, effectIds[i]);
    }

    [Test]
    public void MovePointUpEffect_AppliesValueRateTimesCountRateToCaster()
    {
        BattleCharacter caster = CreatePlayer("Research_MovePoint_Caster", 20);

        new MovePointUpEffect().Execute(new BattleEffectContext
        {
            PlayerCaster = caster,
            Value = 2,
            Count = 2
        });

        Assert.That(caster.RuntimeData.StatusEffects, Has.Count.EqualTo(1));
        Assert.That(caster.RuntimeData.StatusEffects[0].EffectId, Is.EqualTo("E_Move_Point_Up"));
        Assert.That(caster.RuntimeData.StatusEffects[0].Stack, Is.EqualTo(4));
        Assert.That(caster.RuntimeData.StatusEffects[0].TurnCount, Is.EqualTo(1));
    }

    [Test]
    public void EquipmentResearchEffects_ModifyPlayerEffectValuesAndBattleStats()
    {
        CharacterRuntimeData runtime = CreateRuntime("Research_Modifier", 50);
        runtime.StatusEffects.Add(new StatusEffectRuntimeData("E_Poison_Apply_Double", 1));
        runtime.StatusEffects.Add(new StatusEffectRuntimeData("E_Bleeding_Apply_Double", 1));
        runtime.StatusEffects.Add(new StatusEffectRuntimeData("E_Buff_Apply_Double", 1));
        runtime.StatusEffects.Add(new StatusEffectRuntimeData("E_Max_HP_Up", 1));
        runtime.StatusEffects.Add(new StatusEffectRuntimeData("E_Move_Point_Up", 2));
        runtime.StatusEffects.Add(new StatusEffectRuntimeData("E_Skill_Resource_Gain_Up", 3));

        PlayerReservedCommand debuffCommand = new(runtime, CreateSkill("S_Poison", SkillType.Debuff));
        PlayerReservedCommand buffCommand = new(runtime, CreateSkill("S_Boost", SkillType.Buff));

        Assert.That(
            BattleEquipmentEffectService.ModifyPlayerEffectValue(
                runtime,
                debuffCommand,
                CreateEntry("E_Poison", 2, 2),
                2),
            Is.EqualTo(4));

        Assert.That(
            BattleEquipmentEffectService.ModifyPlayerEffectValue(
                runtime,
                debuffCommand,
                CreateEntry("E_Bleed", 3, 1),
                3),
            Is.EqualTo(6));

        Assert.That(
            BattleEquipmentEffectService.ModifyPlayerEffectValue(
                runtime,
                buffCommand,
                CreateEntry("E_Boost", 1, 2),
                1),
            Is.EqualTo(2));

        CharacterMasterData master = new()
        {
            CharacterId = runtime.CharacterId,
            MaxHP = 100,
            MaxCost = 5,
            CostRecovery = 1
        };

        Assert.That(BattleEquipmentEffectService.GetEffectiveMaxHP(runtime, master), Is.EqualTo(110));
        Assert.That(BattleEquipmentEffectService.GetEffectiveCostRecovery(runtime, master), Is.EqualTo(4));
        Assert.That(BattleEquipmentEffectService.GetEffectiveMoveValue(runtime, master), Is.EqualTo(2));
    }

    [Test]
    public void StrikeEffect_AppliesMoveFirstAttackAndLowHpPowerWithoutConsumingStacks()
    {
        BattleCharacter caster = CreatePlayer("Research_Attacker", 100);
        MonsterUnit target = CreateMonster("Research_Target", 100);
        caster.RuntimeData.CurrentHP = 50;
        caster.RuntimeData.StatusEffects.Add(new StatusEffectRuntimeData("E_Move_First_Attack_Power", 1));
        caster.RuntimeData.StatusEffects.Add(new StatusEffectRuntimeData("E_Low_HP_Power", 20));
        BattleEquipmentEffectService.MarkMovedBeforeNextAttack(caster.RuntimeData);

        new StrikeEffect().Execute(new BattleEffectContext
        {
            PlayerCaster = caster,
            MonsterTarget = target,
            PlayerSkillData = CreateSkill("S_Strike", SkillType.Attack),
            Value = 10
        });

        Assert.That(target.RuntimeData.CurrentHP, Is.EqualTo(86));
        Assert.That(GetStatusStack(caster.RuntimeData, "E_Move_First_Attack_Power"), Is.EqualTo(1));
        Assert.That(BattleEquipmentEffectService.IsMoveFirstAttackPowerReady(caster.RuntimeData), Is.True);

        BattleEquipmentEffectService.ClearMoveFirstAttackPowerIfAttack(
            caster.RuntimeData,
            CreateSkill("S_Strike", SkillType.Attack));

        Assert.That(BattleEquipmentEffectService.IsMoveFirstAttackPowerReady(caster.RuntimeData), Is.False);
        Assert.That(GetStatusStack(caster.RuntimeData, "E_Move_First_Attack_Power"), Is.EqualTo(1));
    }

    [Test]
    public void StrikeEffect_HealsPlayerOnKillWhenKillHealIsActive()
    {
        BattleCharacter caster = CreatePlayer("Research_KillHeal_Attacker", 100);
        MonsterUnit target = CreateMonster("Research_KillHeal_Target", 5);
        caster.RuntimeData.CurrentHP = 10;
        caster.RuntimeData.StatusEffects.Add(new StatusEffectRuntimeData("E_Kill_Heal", 1));

        new StrikeEffect().Execute(new BattleEffectContext
        {
            PlayerCaster = caster,
            MonsterTarget = target,
            PlayerSkillData = CreateSkill("S_Kill", SkillType.Attack),
            Value = 10
        });

        Assert.That(target.RuntimeData.IsDead, Is.True);
        Assert.That(caster.RuntimeData.CurrentHP, Is.EqualTo(15));
    }

    private BattleCharacter CreatePlayer(string characterId, int hp)
    {
        GameObject go = CreateObject(characterId);
        BattleCharacter character = go.AddComponent<BattleCharacter>();
        character.Initialize(CreateRuntime(characterId, hp));
        return character;
    }

    private MonsterUnit CreateMonster(string runtimeId, int hp)
    {
        GameObject go = CreateObject(runtimeId);
        MonsterUnit monster = go.AddComponent<MonsterUnit>();
        MonsterMasterData masterData = new()
        {
            MonsterId = runtimeId,
            Name = runtimeId,
            HP = hp
        };

        monster.Initialize(new MonsterRuntimeData(runtimeId, masterData));
        return monster;
    }

    private GameObject CreateObject(string objectName)
    {
        GameObject go = new(objectName);
        createdObjects.Add(go);
        return go;
    }

    private static CharacterRuntimeData CreateRuntime(string characterId, int hp)
    {
        return new CharacterRuntimeData
        {
            CharacterId = characterId,
            MaxHP = hp,
            CurrentHP = hp,
            MaxCost = 5,
            CurrentCost = 5,
            CostRecovery = 1
        };
    }

    private static SkillMasterData CreateSkill(string skillId, SkillType skillType)
    {
        return new SkillMasterData
        {
            SkillId = skillId,
            Name = skillId,
            SkillType = skillType,
            Target = TargetType.EnemyParty
        };
    }

    private static SkillEffectEntry CreateEntry(string effectId, int value, int count)
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

    private static void DestroyIfExists(string objectName)
    {
        GameObject go = GameObject.Find(objectName);

        if (go != null)
            Object.DestroyImmediate(go);
    }
}

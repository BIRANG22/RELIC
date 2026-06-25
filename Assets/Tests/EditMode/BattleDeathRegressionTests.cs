using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;

public class BattleDeathRegressionTests
{
    private readonly List<GameObject> createdObjects = new();

    [TearDown]
    public void TearDown()
    {
        BattleEffectUtility.OnPlayerDamaged = null;

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
    public void PlayerDeath_ClearsStatusEffectsAndReservedCosts()
    {
        BattleCharacter character = CreateCharacter("Char_Death_Clear", 5, 12);
        CharacterRuntimeData runtime = character.RuntimeData;
        runtime.CurrentShield = 3;
        runtime.CurrentCost = 5;
        runtime.CurrentResource = 2;
        runtime.StatusEffects.Add(new StatusEffectRuntimeData("E_Power", 2));
        runtime.StatusEffects.Add(new StatusEffectRuntimeData("E_Burn", 1));
        runtime.AddReservedHP(1);
        runtime.AddReservedCost(2);
        runtime.AddReservedResource(1);
        runtime.AddReservedShield(1);

        BattleEffectUtility.DamagePlayer(character, 20);

        Assert.That(runtime.CurrentHP, Is.Zero);
        Assert.That(runtime.CurrentShield, Is.Zero);
        Assert.That(runtime.StatusEffects, Is.Empty);
        Assert.That(runtime.ReservedHPCost, Is.Zero);
        Assert.That(runtime.ReservedCost, Is.Zero);
        Assert.That(runtime.ReservedResourceCost, Is.Zero);
        Assert.That(runtime.ReservedShieldCost, Is.Zero);
    }

    [Test]
    public void BattleStartEffects_RevivesDeadPlayerWithOneHP()
    {
        CharacterRuntimeData runtime = CreateCharacterRuntime("Char_Next_Battle_Revive", 0);
        runtime.MaxHP = 8;
        runtime.CurrentHP = 0;

        CharacterMasterData masterData = new()
        {
            CharacterId = runtime.CharacterId,
            MaxHP = 12,
            MaxCost = 5,
            CostRecovery = 2,
            MoveValue = 10
        };

        BattleEquipmentEffectService.ApplyBattleStartEffects(runtime, masterData);

        Assert.That(runtime.MaxHP, Is.EqualTo(12));
        Assert.That(runtime.CurrentHP, Is.EqualTo(1));
        Assert.That(runtime.IsDead, Is.False);
    }

    [Test]
    public void DeadPlayer_StillOccupiesGrid()
    {
        BattleCharacter character = CreateCharacter("Char_Dead_Occupies", 0, 19);

        Assert.That(character.RuntimeData.CurrentHP, Is.Zero);
        Assert.That(BattleOccupancyService.IsOccupiedByCharacter(19), Is.True);
    }

    [Test]
    public void MonsterRangeFilter_DoesNotTargetDeadPlayerOnGrid()
    {
        CreateCharacter("Char_Dead_Target", 0, 7);
        MonsterSkillData skill = new()
        {
            SkillId = "M_Target_Dead_Player",
            Target = TargetType.PlayerParty
        };

        List<int> filtered = MonsterSkillRangeService.FilterTargetGridIndices(
            skill,
            new List<int> { 7 });

        Assert.That(filtered, Is.Empty);
    }

    [Test]
    public void MonsterSkillEffect_DoesNotApplyToDeadPlayers()
    {
        BattleCharacter deadTarget = CreateCharacter("Char_Dead_Effect", 0, 11);
        MonsterUnit caster = CreateMonster("Monster_Caster", 10);
        MonsterSkillData skill = new()
        {
            SkillId = "M_Recharge_Dead_Player",
            Target = TargetType.PlayerParty,
            EffectIds = "E_Recharge",
            ValueRate = "1",
            CountRate = "1"
        };
        MonsterReservedCommand command = new(caster.RuntimeData, skill);
        command.SetRangeResult(new List<int> { 11 }, new List<int> { 11 });
        MonsterSkillEffectService service = new(
            new BattleDamageService(null),
            new BattleDeathService(null, null, null),
            new BattleHUDService(),
            null);

        service.ApplyMonsterSkill(caster, command);

        Assert.That(deadTarget.RuntimeData.StatusEffects, Is.Empty);
    }

    [Test]
    public void TurnEndStatusEffects_DoNotRecoverCostForDeadPlayers()
    {
        BattleCharacter character = CreateCharacter("Char_Dead_Turn_End", 0, 8);
        character.RuntimeData.MaxCost = 5;
        character.RuntimeData.CurrentCost = 0;
        character.RuntimeData.StatusEffects.Add(new StatusEffectRuntimeData("E_Recharge", 1));
        BattleStatusEffectService service = new(
            new BattleDamageService(null),
            new BattleDeathService(null, null, null));

        service.ApplyTurnEndEffects();

        Assert.That(character.RuntimeData.CurrentCost, Is.Zero);
    }

    [Test]
    public void TimelineReserveBlockReason_RejectsDeadPlayer()
    {
        BattleTimelineController controller =
            CreateObject("Timeline_Dead_Reserve").AddComponent<BattleTimelineController>();
        CharacterRuntimeData runtime = CreateCharacterRuntime("Char_Dead_Reserve", 0);
        PlayerReservedCommand command = new(runtime, CreateSkill("S_Dead_Reserve"));

        string blockReason = InvokePrivateMethod<string>(
            controller,
            "GetReserveBlockReason",
            command);

        Assert.That(blockReason, Is.Not.Empty);
    }

    [Test]
    public void BattleActionBatchBuilder_SkipsDeadPlayerCommands()
    {
        BattleTimelineController controller =
            CreateObject("Timeline_Dead_Batch").AddComponent<BattleTimelineController>();
        ReserveTurnSlotUI slot =
            CreateObject("Timeline_Dead_Batch_Slot").AddComponent<ReserveTurnSlotUI>();
        SetPrivateField(controller, "reserveSlots", new[] { slot });

        BattleCharacter character = CreateCharacter("Char_Dead_Batch", 0, 5);
        PlayerReservedCommand command = new(
            character.RuntimeData,
            CreateSkill("S_Dead_Batch"));
        command.SetDirectionResult(
            BattleDirection.Right,
            new List<int> { 5 },
            new List<int> { 5 });
        Assert.That(slot.AddCommand(command), Is.True);

        List<BattleActionBatch> batches =
            new BattleActionBatchBuilder(null).Build(controller);

        int playerCommandCount = 0;
        for (int i = 0; i < batches.Count; i++)
            playerCommandCount += batches[i].PlayerCommands.Count;

        Assert.That(playerCommandCount, Is.Zero);
    }

    [Test]
    public void MonsterDeathRoutine_WaitsBeforeDestroyingMonster()
    {
        MonsterUnit monster = CreateMonster("Monster_Death_Routine", 0);
        BattleDeathService service = new(null, null, null);

        IEnumerator routine = service.HandleMonsterDeadRoutine(monster);

        Assert.That(routine.MoveNext(), Is.True);
        Assert.That(routine.Current, Is.TypeOf<WaitForSeconds>());
        Assert.That(monster.RuntimeData.IsDeathHandled, Is.True);
        Assert.That(monster.gameObject, Is.Not.Null);
    }

    [Test]
    public void DebugKillAllMonsters_MarksMonstersDead()
    {
        BattleDebugKillAllMonsters debug =
            CreateObject("Debug_Kill_All").AddComponent<BattleDebugKillAllMonsters>();
        MonsterUnit first = CreateMonster("Monster_Debug_Kill_01", 5);
        MonsterUnit second = CreateMonster("Monster_Debug_Kill_02", 7);
        MonsterRuntimeData firstRuntime = first.RuntimeData;
        MonsterRuntimeData secondRuntime = second.RuntimeData;
        first.RuntimeData.CurrentShield = 2;
        second.RuntimeData.CurrentShield = 3;

        debug.KillAllMonstersForDebug();

        Assert.That(firstRuntime.IsDead, Is.True);
        Assert.That(secondRuntime.IsDead, Is.True);
        Assert.That(firstRuntime.IsDeathHandled, Is.True);
        Assert.That(secondRuntime.IsDeathHandled, Is.True);
        Assert.That(firstRuntime.CurrentShield, Is.Zero);
        Assert.That(secondRuntime.CurrentShield, Is.Zero);
    }

    [Test]
    public void DebugDamagePlayers_DamagesOnlyAlivePlayers()
    {
        BattleDebugKillAllMonsters debug =
            CreateObject("Debug_Damage_Players").AddComponent<BattleDebugKillAllMonsters>();
        SetPrivateField(debug, "debugPlayerDamage", 2);
        BattleCharacter alive = CreateCharacter("Char_Debug_Damage_Alive", 5, 21);
        BattleCharacter dead = CreateCharacter("Char_Debug_Damage_Dead", 0, 22);

        debug.DamagePlayersForDebug();

        Assert.That(alive.RuntimeData.CurrentHP, Is.EqualTo(3));
        Assert.That(dead.RuntimeData.CurrentHP, Is.Zero);
    }

    private BattleCharacter CreateCharacter(string characterId, int hp, int gridIndex)
    {
        GameObject go = CreateObject(characterId);
        BattleCharacter character = go.AddComponent<BattleCharacter>();
        character.Initialize(CreateCharacterRuntime(characterId, hp));
        character.SetGridIndex(gridIndex);
        return character;
    }

    private CharacterRuntimeData CreateCharacterRuntime(string characterId, int hp)
    {
        return new CharacterRuntimeData
        {
            CharacterId = characterId,
            MaxHP = Mathf.Max(1, hp),
            CurrentHP = Mathf.Max(0, hp),
            MaxCost = 10,
            CurrentCost = 5,
            CurrentResource = 2,
            CostRecovery = 1
        };
    }

    private MonsterUnit CreateMonster(string runtimeId, int hp)
    {
        GameObject go = CreateObject(runtimeId);
        MonsterUnit monster = go.AddComponent<MonsterUnit>();
        MonsterMasterData masterData = new()
        {
            MonsterId = "Mon_Test",
            Name = "Test Monster",
            HP = Mathf.Max(1, hp)
        };
        MonsterRuntimeData runtime = new(runtimeId, masterData)
        {
            CurrentHP = Mathf.Max(0, hp)
        };
        monster.Initialize(runtime);
        monster.SetOccupiedCells(new List<int> { 0 });
        return monster;
    }

    private SkillMasterData CreateSkill(string skillId)
    {
        return new SkillMasterData
        {
            SkillId = skillId,
            Name = skillId,
            Target = TargetType.EnemyParty,
            ReferenceResource = ReferenceResource.Cost,
            ResourceCostType = ResourceCostType.None,
            RangeType = RangeType.Direction
        };
    }

    private GameObject CreateObject(string objectName)
    {
        GameObject go = new(objectName);
        createdObjects.Add(go);
        return go;
    }

    private static void DestroyIfExists(string objectName)
    {
        GameObject go = GameObject.Find(objectName);

        if (go != null)
            Object.DestroyImmediate(go);
    }

    private static T InvokePrivateMethod<T>(
        object target,
        string methodName,
        params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, $"{methodName} method is missing.");
        return (T)method.Invoke(target, args);
    }

    private static void SetPrivateField<T>(
        object target,
        string fieldName,
        T value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, $"{fieldName} field is missing.");
        field.SetValue(target, value);
    }
}

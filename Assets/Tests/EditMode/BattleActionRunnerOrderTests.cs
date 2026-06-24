using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public class BattleActionRunnerOrderTests
{
    [Test]
    public void BuildActionRoutines_OrdersMonsterCommandsBeforePlayerCommandsInSameBatch()
    {
        BattleActionBatch batch = new();
        batch.PlayerCommands.Add(CreatePlayerCommand("Char_Test", "S_Player_Test"));
        batch.MonsterCommands.Add(CreateMonsterCommand("Monster_Runtime_Test", "S_Monster_Test"));

        BattleActionRunner runner = new(null);

        MethodInfo method = typeof(BattleActionRunner).GetMethod(
            "BuildActionRoutines",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, "BuildActionRoutines method is missing.");

        object result = method.Invoke(runner, new object[] { batch });
        Assert.That(result, Is.InstanceOf<IList>());

        IList routines = (IList)result;
        Assert.That(routines, Has.Count.EqualTo(2));

        Assert.That(GetActionRoutineLabel(routines[0]), Does.StartWith("Monster:"));
        Assert.That(GetActionRoutineLabel(routines[1]), Does.StartWith("PlayerSkill:"));
    }

    [Test]
    public void BuildActionRoutines_OrdersSwiftPlayerCommandsBeforeMonsterCommandsInSameBatch()
    {
        BattleActionBatch batch = new();
        batch.PlayerCommands.Add(CreatePlayerCommand("Char_Test", "S_Player_Swift_Test", "E_Swift"));
        batch.MonsterCommands.Add(CreateMonsterCommand("Monster_Runtime_Test", "S_Monster_Test"));

        BattleActionRunner runner = new(null);

        MethodInfo method = typeof(BattleActionRunner).GetMethod(
            "BuildActionRoutines",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, "BuildActionRoutines method is missing.");

        object result = method.Invoke(runner, new object[] { batch });
        IList routines = (IList)result;

        Assert.That(routines, Has.Count.EqualTo(2));
        Assert.That(GetActionRoutineLabel(routines[0]), Does.StartWith("PlayerSkill:"));
        Assert.That(GetActionRoutineLabel(routines[1]), Does.StartWith("Monster:"));
    }

    [Test]
    public void BuildBatches_OrdersSwiftPlayerBatchBeforeMonsterBatchInSameSlot()
    {
        GameObject timelineObject = new("TimelineSwiftBeforeMonsterBatchOrder");
        GameObject slotObject = new("SlotSwiftBeforeMonsterBatchOrder");

        try
        {
            BattleTimelineController timeline =
                timelineObject.AddComponent<BattleTimelineController>();
            ReserveTurnSlotUI slot =
                slotObject.AddComponent<ReserveTurnSlotUI>();
            slot.Init(timeline, 0);

            SetPrivateField(timeline, "reserveSlots", new[] { slot });

            int sharedTargetGridIndex = 12;
            PlayerReservedCommand swiftPlayerCommand = CreatePlayerCommand(
                "Char_Test",
                "S_Player_Swift_Test",
                "E_Swift",
                sharedTargetGridIndex);
            MonsterReservedCommand monsterCommand = CreateMonsterCommand(
                "Monster_Runtime_Test",
                "S_Monster_Test",
                sharedTargetGridIndex);

            Assert.That(slot.AddCommand(swiftPlayerCommand), Is.True);
            AddMonsterCommandDirectly(timeline, 0, monsterCommand);

            List<BattleActionBatch> batches = new BattleActionBatchBuilder(null).Build(timeline);

            Assert.That(batches, Has.Count.GreaterThanOrEqualTo(2));
            Assert.That(batches[0].PlayerCommands, Has.Count.EqualTo(1));
            Assert.That(batches[0].MonsterCommands, Is.Empty);
            Assert.That(batches[1].MonsterCommands, Has.Count.EqualTo(1));
        }
        finally
        {
            Object.DestroyImmediate(slotObject);
            Object.DestroyImmediate(timelineObject);
        }
    }

    [Test]
    public void BuildBatches_OrdersMonsterBatchBeforeNonSwiftPlayerBatchInSameSlot()
    {
        GameObject timelineObject = new("TimelineMonsterBeforeNormalBatchOrder");
        GameObject slotObject = new("SlotMonsterBeforeNormalBatchOrder");

        try
        {
            BattleTimelineController timeline =
                timelineObject.AddComponent<BattleTimelineController>();
            ReserveTurnSlotUI slot =
                slotObject.AddComponent<ReserveTurnSlotUI>();
            slot.Init(timeline, 0);

            SetPrivateField(timeline, "reserveSlots", new[] { slot });

            int sharedTargetGridIndex = 12;
            PlayerReservedCommand playerCommand = CreatePlayerCommand(
                "Char_Test",
                "S_Player_Normal_Test",
                null,
                sharedTargetGridIndex);
            MonsterReservedCommand monsterCommand = CreateMonsterCommand(
                "Monster_Runtime_Test",
                "S_Monster_Test",
                sharedTargetGridIndex);

            Assert.That(slot.AddCommand(playerCommand), Is.True);
            AddMonsterCommandDirectly(timeline, 0, monsterCommand);

            List<BattleActionBatch> batches = new BattleActionBatchBuilder(null).Build(timeline);

            Assert.That(batches, Has.Count.GreaterThanOrEqualTo(2));
            Assert.That(batches[0].MonsterCommands, Has.Count.EqualTo(1));
            Assert.That(batches[0].PlayerCommands, Is.Empty);
            Assert.That(batches[1].PlayerCommands, Has.Count.EqualTo(1));
        }
        finally
        {
            Object.DestroyImmediate(slotObject);
            Object.DestroyImmediate(timelineObject);
        }
    }

    private static PlayerReservedCommand CreatePlayerCommand(
        string characterId,
        string skillId,
        string effectIds = null,
        int targetGridIndex = -1)
    {
        CharacterRuntimeData runtime = new()
        {
            CharacterId = characterId
        };

        SkillMasterData skill = new()
        {
            SkillId = skillId,
            Category = Category.Ability,
            EffectIds = effectIds
        };

        PlayerReservedCommand command = new(runtime, skill);

        if (targetGridIndex >= 0)
        {
            command.SetDirectionResult(
                BattleDirection.Right,
                new List<int> { targetGridIndex },
                new List<int> { targetGridIndex });
        }

        return command;
    }

    private static MonsterReservedCommand CreateMonsterCommand(
        string runtimeId,
        string skillId,
        int targetGridIndex = -1)
    {
        MonsterMasterData masterData = new()
        {
            MonsterId = "Monster_Test",
            Name = "Monster Test",
            HP = 10
        };

        MonsterRuntimeData runtime = new(runtimeId, masterData);
        MonsterSkillData skill = new()
        {
            SkillId = skillId
        };

        MonsterReservedCommand command = new(runtime, skill);

        if (targetGridIndex >= 0)
        {
            command.SetRangeResult(
                new List<int> { targetGridIndex },
                new List<int> { targetGridIndex });
        }

        return command;
    }

    private static void AddMonsterCommandDirectly(
        BattleTimelineController timeline,
        int slotIndex,
        MonsterReservedCommand command)
    {
        FieldInfo field = typeof(BattleTimelineController).GetField(
            "monsterCommandsBySlot",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, "monsterCommandsBySlot field is missing.");

        List<MonsterReservedCommand>[] commandsBySlot =
            (List<MonsterReservedCommand>[])field.GetValue(timeline);

        Assert.That(commandsBySlot, Is.Not.Null);
        Assert.That(slotIndex, Is.InRange(0, commandsBySlot.Length - 1));

        if (commandsBySlot[slotIndex] == null)
            commandsBySlot[slotIndex] = new List<MonsterReservedCommand>();

        commandsBySlot[slotIndex].Add(command);
    }

    private static string GetActionRoutineLabel(object actionRoutine)
    {
        Assert.That(actionRoutine, Is.Not.Null);

        FieldInfo field = actionRoutine.GetType().GetField(
            "Label",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, "ActionRoutine.Label field is missing.");
        return (string)field.GetValue(actionRoutine);
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, $"{fieldName} field is missing.");
        field.SetValue(target, value);
    }
}

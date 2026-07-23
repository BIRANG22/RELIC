using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public class MonsterMoveRegressionTests
{
    [Test]
    public void MonsterMoveSimulation_BlocksDiagonalMoveWhenBothAxisRoutesAreBlocked()
    {
        GridManager gridManager =
            new GameObject("GridManagerMonsterDiagonalBlocked").AddComponent<GridManager>();

        try
        {
            BattleActionSimulationService simulator = new(gridManager);
            Dictionary<string, int> playerPositions =
                GetPrivateField<Dictionary<string, int>>(simulator, "playerPositions");
            Dictionary<string, List<int>> monsterPositions =
                GetPrivateField<Dictionary<string, List<int>>>(simulator, "monsterPositions");

            int monsterStartIndex = gridManager.CoordToIndex(new Vector2Int(0, 0));
            int xAxisBlockIndex = gridManager.CoordToIndex(new Vector2Int(1, 0));
            int yAxisBlockIndex = gridManager.CoordToIndex(new Vector2Int(0, 1));
            string monsterRuntimeId = "Monster_Diagonal_Test";

            playerPositions["Char_X_Block"] = xAxisBlockIndex;
            playerPositions["Char_Y_Block"] = yAxisBlockIndex;
            monsterPositions[monsterRuntimeId] = new List<int> { monsterStartIndex };

            MonsterReservedCommand command = CreateMonsterMoveCommand(
                monsterRuntimeId,
                new Vector2Int(1, 1));

            InvokePrivateMethod(
                simulator,
                "SimulateMonsterMove",
                command,
                monsterPositions[monsterRuntimeId]);

            Assert.That(command.IsSimulatedMoveBlocked, Is.True);
            Assert.That(command.EffectiveMoveOffset, Is.EqualTo(Vector2Int.zero));
            Assert.That(monsterPositions[monsterRuntimeId], Is.EqualTo(new List<int>
            {
                monsterStartIndex
            }));
        }
        finally
        {
            Object.DestroyImmediate(gridManager.gameObject);
        }
    }

    [Test]
    public void MonsterSkillSimulation_PreservesExplicitMultiGridRange()
    {
        GridManager gridManager =
            new GameObject("GridManagerMonsterExplicitRange").AddComponent<GridManager>();

        try
        {
            BattleActionSimulationService simulator = new(gridManager);
            Dictionary<string, List<int>> monsterPositions =
                GetPrivateField<Dictionary<string, List<int>>>(simulator, "monsterPositions");

            string monsterRuntimeId = "Monster_Explicit_Range_Test";
            int monsterStartIndex = gridManager.CoordToIndex(new Vector2Int(2, 2));
            int targetA = gridManager.CoordToIndex(new Vector2Int(0, 1));
            int targetB = gridManager.CoordToIndex(new Vector2Int(4, 1));
            int targetC = gridManager.CoordToIndex(new Vector2Int(1, 4));
            List<int> explicitTargets = new() { targetA, targetB, targetC };

            monsterPositions[monsterRuntimeId] = new List<int> { monsterStartIndex };

            MonsterReservedCommand command = CreateMonsterAttackCommand(monsterRuntimeId);
            command.SetRangeOriginGridIndex(targetA);
            command.SetExplicitRangeResult(explicitTargets, explicitTargets);

            InvokePrivateMethod(
                simulator,
                "SimulateMonsterSkillRange",
                command,
                monsterPositions[monsterRuntimeId]);

            Assert.That(command.HasExplicitRangeResult, Is.True);
            Assert.That(command.RangeGridIndices, Is.EquivalentTo(explicitTargets));
            Assert.That(command.TargetGridIndices, Is.EquivalentTo(explicitTargets));
        }
        finally
        {
            Object.DestroyImmediate(gridManager.gameObject);
        }
    }

    private static MonsterReservedCommand CreateMonsterMoveCommand(
        string runtimeId,
        Vector2Int moveOffset)
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
            SkillId = "S_Monster_Move_Test",
            TimelineNotation = TimelineActionType.Move
        };
        MonsterReservedCommand command = new(runtime, skill);
        command.SetMoveOffset(moveOffset);
        return command;
    }

    private static MonsterReservedCommand CreateMonsterAttackCommand(string runtimeId)
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
            SkillId = "S_Monster_Explicit_Range_Test",
            Target = TargetType.PlayerParty,
            TimelineNotation = TimelineActionType.Attack,
            RangeId = "Range_Self"
        };
        return new MonsterReservedCommand(runtime, skill);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, $"{fieldName} field is missing.");
        return (T)field.GetValue(target);
    }

    private static object InvokePrivateMethod(
        object target,
        string methodName,
        params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, $"{methodName} method is missing.");
        return method.Invoke(target, args);
    }
}

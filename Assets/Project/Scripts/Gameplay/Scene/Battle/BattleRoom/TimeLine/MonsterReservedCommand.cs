using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;

public class MonsterReservedCommand
{
    public MonsterRuntimeData UserRuntime { get; private set; }
    public MonsterSkillData SkillData { get; private set; }

    public string RuntimeId => UserRuntime != null ? UserRuntime.RuntimeId : "";
    public string MonsterId => UserRuntime != null ? UserRuntime.MonsterId : "";
    public string SkillId => SkillData != null ? SkillData.SkillId : "";

    public Vector2Int MoveOffset { get; private set; } = Vector2Int.zero;

    public List<int> RangeGridIndices { get; private set; } = new();
    public List<int> TargetGridIndices { get; private set; } = new();

    public MonsterReservedCommand(MonsterRuntimeData userRuntime, MonsterSkillData skillData)
    {
        UserRuntime = userRuntime;
        SkillData = skillData;
    }

    public bool HasSimulatedResult { get; private set; }
    public bool IsSimulatedMoveBlocked { get; private set; }
    public Vector2Int SimulatedMoveOffset { get; private set; } = Vector2Int.zero;

    public Vector2Int EffectiveMoveOffset =>
        HasSimulatedResult ? SimulatedMoveOffset : MoveOffset;

    public void SetSimulatedMoveResult(bool blocked, Vector2Int moveOffset)
    {
        HasSimulatedResult = true;
        IsSimulatedMoveBlocked = blocked;
        SimulatedMoveOffset = moveOffset;
    }
    public void SetMoveOffset(Vector2Int moveOffset)
    {
        MoveOffset = moveOffset;
    }

    public void SetRangeResult(List<int> rangeGridIndices, List<int> targetGridIndices = null)
    {
        RangeGridIndices = rangeGridIndices != null
            ? new List<int>(rangeGridIndices)
            : new List<int>();

        TargetGridIndices = targetGridIndices != null
            ? new List<int>(targetGridIndices)
            : new List<int>(RangeGridIndices);
    }
}
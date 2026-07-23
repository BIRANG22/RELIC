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

    public int ReservedDamage { get; private set; } = -1;
    public bool HasReservedDamage => ReservedDamage > 0;
    public int ActionIndex { get; private set; }
    public int RangeOriginGridIndex { get; private set; } = -1;
    public bool HasForcedDirection { get; private set; }
    public BattleDirection ForcedDirection { get; private set; } = BattleDirection.Right;
    public bool IsPortalMove { get; private set; }
    public bool HasExplicitRangeResult { get; private set; }

    public List<int> RangeGridIndices { get; private set; } = new();
    public List<int> TargetGridIndices { get; private set; } = new();

    public MonsterReservedCommand(MonsterRuntimeData userRuntime, MonsterSkillData skillData)
    {
        UserRuntime = userRuntime;
        SkillData = skillData;
        SetActionIndex(userRuntime != null && skillData != null
            ? userRuntime.GetPresentationActionIndexForSkill(skillData.SkillId)
            : 0);
        ReserveDamage();
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

    public void SetActionIndex(int actionIndex)
    {
        ActionIndex = Mathf.Clamp(actionIndex, 0, MonsterMasterData.PossibleSkillSlotCount);
    }

    public void SetRangeOriginGridIndex(int gridIndex)
    {
        RangeOriginGridIndex = Mathf.Max(-1, gridIndex);
    }

    public void SetForcedDirection(BattleDirection direction)
    {
        HasForcedDirection = true;
        ForcedDirection = direction;
    }

    public void ClearForcedDirection()
    {
        HasForcedDirection = false;
    }

    public void SetPortalMove(bool isPortalMove)
    {
        IsPortalMove = isPortalMove;
    }

    public int EnsureReservedDamage()
    {
        if (!BattleDamageService.ShouldReserveMonsterDamage(SkillData))
            return 0;

        if (!HasReservedDamage)
            ReserveDamage();

        return ReservedDamage;
    }

    public void ReserveDamage()
    {
        if (!BattleDamageService.ShouldReserveMonsterDamage(SkillData))
        {
            ReservedDamage = -1;
            return;
        }

        ReservedDamage = BattleDamageService.RollMonsterDamage(SkillData);
    }

    public void SetReservedDamage(int damage)
    {
        ReservedDamage = Mathf.Max(1, damage);
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

    public void SetExplicitRangeResult(List<int> rangeGridIndices, List<int> targetGridIndices = null)
    {
        HasExplicitRangeResult = true;
        SetRangeResult(rangeGridIndices, targetGridIndices);
    }
}

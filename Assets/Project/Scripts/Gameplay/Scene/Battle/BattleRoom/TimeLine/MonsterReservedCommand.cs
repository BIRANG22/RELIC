using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;

public enum MonsterAttackSpatialPolicy
{
    ActorRelative = 0,
    LockedTargetGrid = 1
}

public static class MonsterAttackSpatialPolicyResolver
{
    private const string LockedTargetRangePrefix = "Range_m_";

    public static MonsterAttackSpatialPolicy Resolve(string rangeId)
    {
        return !string.IsNullOrWhiteSpace(rangeId) &&
               rangeId.StartsWith(LockedTargetRangePrefix, System.StringComparison.OrdinalIgnoreCase)
            ? MonsterAttackSpatialPolicy.LockedTargetGrid
            : MonsterAttackSpatialPolicy.ActorRelative;
    }

    public static MonsterAttackSpatialPolicy ResolveSaved(int serializedPolicy, string rangeId)
    {
        MonsterAttackSpatialPolicy dataPolicy = Resolve(rangeId);
        if (dataPolicy == MonsterAttackSpatialPolicy.LockedTargetGrid)
            return dataPolicy;

        return serializedPolicy == (int)MonsterAttackSpatialPolicy.LockedTargetGrid
            ? MonsterAttackSpatialPolicy.LockedTargetGrid
            : MonsterAttackSpatialPolicy.ActorRelative;
    }
}

public static class BattleAttackExecutionDirectionPolicy
{
    public static BattleDirection ResolveExecutionDirection(
        BattleDirection reservedDirection,
        BattleDirection liveDirection)
    {
        _ = reservedDirection;
        return liveDirection;
    }

    public static bool ShouldPreserveMonsterTargetGrid(MonsterAttackSpatialPolicy spatialPolicy)
    {
        return spatialPolicy == MonsterAttackSpatialPolicy.LockedTargetGrid;
    }

    public static int ResolveExecutionOriginGridIndex(
        MonsterAttackSpatialPolicy spatialPolicy,
        int liveOriginGridIndex,
        int reservedOriginGridIndex)
    {
        return ShouldPreserveMonsterTargetGrid(spatialPolicy)
            ? reservedOriginGridIndex
            : liveOriginGridIndex;
    }
}

public static class BattleMonsterCommandRangeSnapshotPolicy
{
    public static void Restore(
        MonsterReservedCommand command,
        bool hasExplicitRangeResult,
        MonsterAttackSpatialPolicy spatialPolicy,
        List<int> rangeGridIndices,
        List<int> targetGridIndices)
    {
        if (command == null)
            return;

        command.SetAttackSpatialPolicy(spatialPolicy);

        if (hasExplicitRangeResult)
        {
            command.SetExplicitRangeResult(rangeGridIndices, targetGridIndices);
            return;
        }

        command.SetRangeResult(rangeGridIndices, targetGridIndices);
    }
}

public class MonsterReservedCommand
{
    public MonsterRuntimeData UserRuntime { get; private set; }
    public MonsterSkillData SkillData { get; private set; }

    public string RuntimeId => UserRuntime != null ? UserRuntime.RuntimeId : "";
    public string MonsterId => UserRuntime != null ? UserRuntime.MonsterId : "";
    public string SkillId => SkillData != null ? SkillData.SkillId : "";
    public int TimelineEntryCost => BattleTimelineCapacityPolicy.NormalizeOccupiedOrders(
        SkillData != null ? SkillData.TimelineEntryCost : 1);

    public Vector2Int MoveOffset { get; private set; } = Vector2Int.zero;

    public int ReservedDamage { get; private set; } = -1;
    public bool HasReservedDamage => ReservedDamage > 0;
    public int ActionIndex { get; private set; }
    public int RangeOriginGridIndex { get; private set; } = -1;
    public int RangeOriginCasterGridIndex { get; private set; } = -1;
    public bool HasForcedDirection { get; private set; }
    public BattleDirection ForcedDirection { get; private set; } = BattleDirection.Right;
    public bool IsPortalMove { get; private set; }
    public bool HasExplicitRangeResult { get; private set; }
    public MonsterAttackSpatialPolicy AttackSpatialPolicy { get; private set; }

    public List<int> RangeGridIndices { get; private set; } = new();
    public List<int> TargetGridIndices { get; private set; } = new();

    public MonsterReservedCommand(MonsterRuntimeData userRuntime, MonsterSkillData skillData)
    {
        UserRuntime = userRuntime;
        SkillData = skillData;
        AttackSpatialPolicy = MonsterAttackSpatialPolicyResolver.Resolve(
            skillData != null ? skillData.RangeId : null);
        SetActionIndex(userRuntime != null && skillData != null
            ? userRuntime.GetPresentationActionIndexForSkill(skillData.SkillId)
            : 0);
        ReserveDamage();
    }

    public bool HasSimulatedResult { get; private set; }
    public bool IsSimulatedMoveBlocked { get; private set; }
    public Vector2Int SimulatedMoveOffset { get; private set; } = Vector2Int.zero;

    public bool UseRequestedMoveOffsetForExecution { get; private set; }

    public Vector2Int EffectiveMoveOffset =>
        UseRequestedMoveOffsetForExecution
            ? MoveOffset
            : (HasSimulatedResult ? SimulatedMoveOffset : MoveOffset);

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

    public void SetUseRequestedMoveOffsetForExecution(bool useRequestedMoveOffset)
    {
        UseRequestedMoveOffsetForExecution = useRequestedMoveOffset;
    }

    public void SetActionIndex(int actionIndex)
    {
        ActionIndex = Mathf.Clamp(actionIndex, 0, MonsterMasterData.PossibleSkillSlotCount);
    }

    public void SetRangeOriginGridIndex(int gridIndex)
    {
        RangeOriginGridIndex = Mathf.Max(-1, gridIndex);
    }

    public void SetRangeOriginCasterGridIndex(int gridIndex)
    {
        RangeOriginCasterGridIndex = Mathf.Max(-1, gridIndex);
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

    public void SetAttackSpatialPolicy(MonsterAttackSpatialPolicy spatialPolicy)
    {
        AttackSpatialPolicy = spatialPolicy;
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

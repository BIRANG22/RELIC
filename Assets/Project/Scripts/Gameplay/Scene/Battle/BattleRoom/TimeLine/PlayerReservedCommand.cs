using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;

public enum BattleDirection
{
    Right,
    Left
}

public class PlayerReservedCommand
{
    public CharacterRuntimeData UserRuntime { get; private set; }
    public SkillMasterData SkillData { get; private set; }

    public int HPCost { get; private set; }
    public int Cost { get; private set; }
    public int ResourceCost { get; private set; }
    public int MoveCost { get; private set; }
    public int ShieldCost { get; private set; }

    public int BaseHPCost { get; private set; }
    public int BaseCost { get; private set; }
    public int BaseResourceCost { get; private set; }
    public int BaseMoveCost { get; private set; }
    public int BaseShieldCost { get; private set; }

    public int TimelineSlotIndex { get; private set; } = -1;
    public bool ReservationCostModifiersApplied { get; private set; }

    public BattleDirection Direction { get; private set; } = BattleDirection.Right;
    public int SelectedGridIndex { get; private set; } = -1;

    public List<int> RangeGridIndices { get; private set; } = new();
    public List<int> TargetGridIndices { get; private set; } = new();

    public int ReservedMoveGridIndex { get; private set; } = -1;

    public string CharacterId => UserRuntime != null ? UserRuntime.CharacterId : "";
    public string SkillId => SkillData != null ? SkillData.SkillId : "";
    public string SkillName => SkillData != null ? SkillData.Name : "";

    public Vector2Int MoveOffset { get; private set; } = Vector2Int.zero;

    public Vector2Int VisualMoveOffset { get; private set; } = Vector2Int.zero;
    public int VisualMoveGridIndex { get; private set; } = -1;
    private readonly List<Vector2Int> visualMoveSteps = new();

    public int PlannedMoveDistance { get; private set; }
    public int MoveDistancePerCost { get; private set; } = 1;
    public int ExecutedMoveDistance { get; private set; } = -1;
    public bool MoveCostConsumed { get; private set; }
    public bool BlockedMoveCostRefundApplied { get; private set; }

    public Vector2Int EffectiveVisualMoveOffset =>
        VisualMoveOffset != Vector2Int.zero ? VisualMoveOffset : EffectiveMoveOffset;

    public int EffectiveVisualMoveGridIndex =>
        VisualMoveGridIndex >= 0 ? VisualMoveGridIndex : EffectiveMoveGridIndex;

    public IReadOnlyList<Vector2Int> VisualMoveSteps => visualMoveSteps;

    public bool HasVisualMoveResult =>
        VisualMoveGridIndex >= 0 && VisualMoveOffset != Vector2Int.zero;

    public bool SkipMoveVisual { get; private set; }

    public void SetSkipMoveVisual(bool skip)
    {
        SkipMoveVisual = skip;
    }
    public void SetVisualMoveResult(
        int visualGridIndex,
        Vector2Int visualMoveOffset,
        IReadOnlyList<Vector2Int> moveSteps = null)
    {
        VisualMoveGridIndex = visualGridIndex;
        VisualMoveOffset = visualMoveOffset;
        visualMoveSteps.Clear();

        if (moveSteps == null)
            return;

        for (int i = 0; i < moveSteps.Count; i++)
            visualMoveSteps.Add(moveSteps[i]);
    }
    public PlayerReservedCommand(CharacterRuntimeData userRuntime, SkillMasterData skillData)
    {
        UserRuntime = userRuntime;
        SkillData = skillData;

        CalculateCosts(skillData);
    }

    public bool HasSimulatedResult { get; private set; }
    public bool IsSimulatedMoveBlocked { get; private set; }
    public Vector2Int SimulatedMoveOffset { get; private set; } = Vector2Int.zero;
    public int SimulatedMoveGridIndex { get; private set; } = -1;

    public Vector2Int EffectiveMoveOffset =>
        HasSimulatedResult ? SimulatedMoveOffset : MoveOffset;

    public int EffectiveMoveGridIndex =>
        HasSimulatedResult ? SimulatedMoveGridIndex : ReservedMoveGridIndex;

    public Vector2Int ExecutionMoveOffset =>
        SkipMoveVisual ? MoveOffset : EffectiveMoveOffset;

    public int PreviewMoveGridIndex =>
        SkipMoveVisual && ReservedMoveGridIndex >= 0
            ? ReservedMoveGridIndex
            : EffectiveMoveGridIndex;

    public bool IsVisualSkipConsumedAtGrid(int gridIndex)
    {
        return SkipMoveVisual &&
               ReservedMoveGridIndex >= 0 &&
               gridIndex == ReservedMoveGridIndex;
    }

    public void SetSimulatedMoveResult(
        bool blocked,
        int gridIndex,
        Vector2Int moveOffset)
    {
        HasSimulatedResult = true;
        IsSimulatedMoveBlocked = blocked;
        SimulatedMoveGridIndex = gridIndex;
        SimulatedMoveOffset = moveOffset;
    }

    public void SetMoveDirection(BattleDirection direction)
    {
        Direction = direction;
    }

    public void SetTimelineSlotIndex(int slotIndex)
    {
        TimelineSlotIndex = slotIndex;
    }

    public void MarkReservationCostModifiersApplied()
    {
        ReservationCostModifiersApplied = true;
    }

    public void ResetCostsToBase()
    {
        HPCost = BaseHPCost;
        Cost = BaseCost;
        ResourceCost = BaseResourceCost;
        MoveCost = BaseMoveCost;
        ShieldCost = BaseShieldCost;
        ReservationCostModifiersApplied = false;
    }

    public void SetBaseCosts(
        int hpCost,
        int cost,
        int resourceCost,
        int moveCost,
        int shieldCost)
    {
        BaseHPCost = Mathf.Max(0, hpCost);
        BaseCost = Mathf.Max(0, cost);
        BaseResourceCost = Mathf.Max(0, resourceCost);
        BaseMoveCost = Mathf.Max(0, moveCost);
        BaseShieldCost = Mathf.Max(0, shieldCost);

        ResetCostsToBase();
    }

    public void SetCosts(
        int hpCost,
        int cost,
        int resourceCost,
        int moveCost,
        int shieldCost)
    {
        HPCost = Mathf.Max(0, hpCost);
        Cost = Mathf.Max(0, cost);
        ResourceCost = Mathf.Max(0, resourceCost);
        MoveCost = Mathf.Max(0, moveCost);
        ShieldCost = Mathf.Max(0, shieldCost);
    }

    public void SetMoveReservationCost(
        int plannedMoveDistance,
        int moveDistancePerCost)
    {
        PlannedMoveDistance = Mathf.Max(0, plannedMoveDistance);
        MoveDistancePerCost = Mathf.Max(1, moveDistancePerCost);

        SetBaseCosts(
            0,
            CalculateMoveCost(PlannedMoveDistance, MoveDistancePerCost),
            0,
            0,
            0
        );
    }

    public void SetExecutedMoveDistance(int executedMoveDistance)
    {
        ExecutedMoveDistance = Mathf.Max(0, executedMoveDistance);
    }

    public void MarkMoveCostConsumed()
    {
        MoveCostConsumed = true;
    }

    public int ApplyBlockedMoveCostRefund()
    {
        if (BlockedMoveCostRefundApplied)
            return 0;

        BlockedMoveCostRefundApplied = true;

        int refund = GetBlockedMoveCostRefund();

        if (refund <= 0 || UserRuntime == null)
            return 0;

        int maxCost = UserRuntime.MaxCost > 0
            ? UserRuntime.MaxCost
            : UserRuntime.CurrentCost + refund;

        UserRuntime.CurrentCost = Mathf.Min(
            maxCost,
            UserRuntime.CurrentCost + refund
        );

        return refund;
    }

    public int GetBlockedMoveCostRefund()
    {
        if (PlannedMoveDistance <= 0 || ExecutedMoveDistance < 0)
            return 0;

        int actualCost = CalculateMoveCost(
            ExecutedMoveDistance,
            MoveDistancePerCost
        );

        int blockedCost = Mathf.Max(0, Cost - actualCost);
        return blockedCost / 2;
    }

    public static int CalculateMoveCost(
        int moveDistance,
        int moveDistancePerCost)
    {
        int safeDistance = Mathf.Max(0, moveDistance);

        if (safeDistance <= 0)
            return 0;

        int safeDistancePerCost = Mathf.Max(1, moveDistancePerCost);
        return Mathf.CeilToInt(safeDistance / (float)safeDistancePerCost);
    }

    public void SetSimulatedRangeResult(
        List<int> rangeGridIndices,
        List<int> targetGridIndices)
    {
        RangeGridIndices = rangeGridIndices != null
            ? new List<int>(rangeGridIndices)
            : new List<int>();

        TargetGridIndices = targetGridIndices != null
            ? new List<int>(targetGridIndices)
            : new List<int>(RangeGridIndices);
    }

    public void SetDirectionResult(
        BattleDirection direction,
        List<int> rangeGridIndices,
        List<int> targetGridIndices)
    {
        Direction = direction;
        SelectedGridIndex = -1;
        ReservedMoveGridIndex = -1;
        ClearVisualMoveResult();

        RangeGridIndices = rangeGridIndices != null ? new List<int>(rangeGridIndices) : new List<int>();
        TargetGridIndices = targetGridIndices != null ? new List<int>(targetGridIndices) : new List<int>();
    }

    public void SetSelectionResult(
    BattleDirection direction,
    int selectedGridIndex,
    List<int> rangeGridIndices,
    Vector2Int moveOffset)
    {
        Direction = direction;
        SelectedGridIndex = selectedGridIndex;
        ReservedMoveGridIndex = selectedGridIndex;
        MoveOffset = moveOffset;
        ClearVisualMoveResult();

        RangeGridIndices = rangeGridIndices != null ? new List<int>(rangeGridIndices) : new List<int>();
        TargetGridIndices = new List<int> { selectedGridIndex };
    }

    private void ClearVisualMoveResult()
    {
        VisualMoveGridIndex = -1;
        VisualMoveOffset = Vector2Int.zero;
        visualMoveSteps.Clear();
        SkipMoveVisual = false;
    }

    private void CalculateCosts(SkillMasterData skillData)
    {
        HPCost = 0;
        Cost = 0;
        ResourceCost = 0;
        MoveCost = 0;
        ShieldCost = 0;
        BaseHPCost = 0;
        BaseCost = 0;
        BaseResourceCost = 0;
        BaseMoveCost = 0;
        BaseShieldCost = 0;

        if (skillData == null)
            return;

        int cost = GetCostValue(skillData);

        switch (skillData.ReferenceResource)
        {
            case ReferenceResource.HP:
                HPCost = cost;
                break;

            case ReferenceResource.Cost:
                Cost = cost;
                break;

            case ReferenceResource.UniqueResource:
                ResourceCost = cost;
                break;

            case ReferenceResource.MovePoint:
                MoveCost = cost;
                break;
        }

        BaseHPCost = HPCost;
        BaseCost = Cost;
        BaseResourceCost = ResourceCost;
        BaseMoveCost = MoveCost;
        BaseShieldCost = ShieldCost;
    }

    private int GetCostValue(SkillMasterData skillData)
    {
        if (skillData == null)
            return 0;

        switch (skillData.ResourceCostType)
        {
            case ResourceCostType.Fixed:
                return Mathf.Max(0, skillData.ResourceCostValue);

            case ResourceCostType.AllCurrent:
                return GetAllCurrentCost(skillData.ReferenceResource);

            default:
                return 0;
        }
    }

    private int GetAllCurrentCost(ReferenceResource resource)
    {
        if (UserRuntime == null)
            return 0;

        switch (resource)
        {
            case ReferenceResource.HP:
                return Mathf.Max(0, UserRuntime.PreviewHP);

            case ReferenceResource.Cost:
                return Mathf.Max(0, UserRuntime.PreviewCost);

            case ReferenceResource.UniqueResource:
                return Mathf.Max(0, UserRuntime.PreviewResource);

            case ReferenceResource.MovePoint:
                return Mathf.Max(0, UserRuntime.PreviewMoveLevel);

            default:
                return 0;
        }
    }
}

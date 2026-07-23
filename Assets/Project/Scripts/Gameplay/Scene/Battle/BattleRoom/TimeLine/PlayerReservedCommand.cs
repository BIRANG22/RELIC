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
    public int ShieldCost { get; private set; }

    public int BaseHPCost { get; private set; }
    public int BaseCost { get; private set; }
    public int BaseResourceCost { get; private set; }
    public int BaseShieldCost { get; private set; }

    public int TimelineSlotIndex { get; private set; } = -1;
    public bool ReservationCostModifiersApplied { get; private set; }
    public bool IsMoveContinuationCommand { get; private set; }
    public bool IsFirstSkillInSlot { get; private set; } = true;
    public bool HadEarlierMoveInSlot { get; private set; }
    public int SameSlotMoveCostBeforeCommand { get; private set; }
    public bool AllyBuffChargeApplied { get; private set; }
    public int MoveReservationCostMultiplier { get; private set; } = 1;

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

    public BattleDirection PreviewMoveDirection => Direction;

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

    public void MergeMoveReservation(PlayerReservedCommand nextCommand)
    {
        if (nextCommand == null || nextCommand.ReservedMoveGridIndex < 0)
            return;

        List<Vector2Int> mergedMoveSteps = new();
        AppendMoveSteps(mergedMoveSteps, this);
        AppendMoveSteps(mergedMoveSteps, nextCommand);

        Vector2Int mergedMoveOffset = MoveOffset + nextCommand.MoveOffset;
        int moveDistancePerCost = nextCommand.MoveDistancePerCost > 0
            ? nextCommand.MoveDistancePerCost
            : MoveDistancePerCost;
        BattleDirection mergedDirection = GetDirectionAfterMoveSteps(
            Direction,
            nextCommand.visualMoveSteps.Count > 0
                ? nextCommand.visualMoveSteps
                : new List<Vector2Int> { nextCommand.MoveOffset });

        SetSelectionResult(
            mergedDirection,
            nextCommand.ReservedMoveGridIndex,
            new List<int> { nextCommand.ReservedMoveGridIndex },
            mergedMoveOffset);
        SetMoveReservationCost(
            GetMoveStepDistance(mergedMoveSteps),
            moveDistancePerCost);
        SetVisualMoveResult(
            nextCommand.ReservedMoveGridIndex,
            mergedMoveOffset,
            mergedMoveSteps);
    }

    private static BattleDirection GetDirectionAfterMove(
        BattleDirection currentDirection,
        Vector2Int moveOffset)
    {
        if (moveOffset.x < 0)
            return BattleDirection.Left;

        if (moveOffset.x > 0)
            return BattleDirection.Right;

        if (moveOffset == Vector2Int.zero)
            return currentDirection == BattleDirection.Right
                ? BattleDirection.Left
                : BattleDirection.Right;

        return currentDirection;
    }

    private static BattleDirection GetDirectionAfterMoveSteps(
        BattleDirection currentDirection,
        IReadOnlyList<Vector2Int> moveSteps)
    {
        if (moveSteps == null || moveSteps.Count <= 0)
            return currentDirection;

        BattleDirection direction = currentDirection;

        for (int i = 0; i < moveSteps.Count; i++)
            direction = GetDirectionAfterMove(direction, moveSteps[i]);

        return direction;
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

    public void SetSlotReservationContext(
        bool isFirstSkillInSlot,
        bool hadEarlierMoveInSlot,
        int sameSlotMoveCostBeforeCommand)
    {
        IsFirstSkillInSlot = isFirstSkillInSlot;
        HadEarlierMoveInSlot = hadEarlierMoveInSlot;
        SameSlotMoveCostBeforeCommand = Mathf.Max(0, sameSlotMoveCostBeforeCommand);
    }

    public bool TryMarkAllyBuffChargeApplied()
    {
        if (AllyBuffChargeApplied)
            return false;

        AllyBuffChargeApplied = true;
        return true;
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
        ShieldCost = BaseShieldCost;
        ReservationCostModifiersApplied = false;
    }

    public void SetBaseCosts(
        int hpCost,
        int cost,
        int resourceCost,
        int shieldCost)
    {
        BaseHPCost = Mathf.Max(0, hpCost);
        BaseCost = Mathf.Max(0, cost);
        BaseResourceCost = Mathf.Max(0, resourceCost);
        BaseShieldCost = Mathf.Max(0, shieldCost);

        ResetCostsToBase();
    }

    public void SetCosts(
        int hpCost,
        int cost,
        int resourceCost,
        int shieldCost)
    {
        HPCost = Mathf.Max(0, hpCost);
        Cost = Mathf.Max(0, cost);
        ResourceCost = Mathf.Max(0, resourceCost);
        ShieldCost = Mathf.Max(0, shieldCost);
    }

    public void SetMoveReservationCostMultiplier(int multiplier)
    {
        MoveReservationCostMultiplier = Mathf.Max(1, multiplier);
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

    public void MarkMoveContinuationCommand()
    {
        IsMoveContinuationCommand = true;
        SetBaseCosts(0, 0, 0, 0);
    }

    public int ApplyBlockedMoveCostRefund()
    {
        if (!MoveCostConsumed)
            return 0;

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

    private static void AppendMoveSteps(
        List<Vector2Int> target,
        PlayerReservedCommand source)
    {
        if (target == null || source == null)
            return;

        if (source.visualMoveSteps.Count > 0)
        {
            for (int i = 0; i < source.visualMoveSteps.Count; i++)
                target.Add(source.visualMoveSteps[i]);

            return;
        }

        AppendUnitMoveSteps(target, source.MoveOffset);
    }

    private static void AppendUnitMoveSteps(List<Vector2Int> target, Vector2Int moveOffset)
    {
        if (target == null)
            return;

        AppendAxisUnitMoveSteps(target, moveOffset.x, true);
        AppendAxisUnitMoveSteps(target, moveOffset.y, false);
    }

    private static void AppendAxisUnitMoveSteps(
        List<Vector2Int> target,
        int amount,
        bool horizontal)
    {
        int remaining = amount;

        while (remaining != 0)
        {
            int step = remaining > 0 ? 1 : -1;
            target.Add(horizontal
                ? new Vector2Int(step, 0)
                : new Vector2Int(0, step));
            remaining -= step;
        }
    }

    private static int GetMoveStepDistance(IReadOnlyList<Vector2Int> moveSteps)
    {
        if (moveSteps == null)
            return 0;

        int total = 0;

        for (int i = 0; i < moveSteps.Count; i++)
            total += Mathf.Abs(moveSteps[i].x) + Mathf.Abs(moveSteps[i].y);

        return total;
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

    public void SetSelectionAreaResult(
        BattleDirection direction,
        int selectedGridIndex,
        List<int> rangeGridIndices)
    {
        Direction = direction;
        SelectedGridIndex = selectedGridIndex;
        ReservedMoveGridIndex = -1;
        MoveOffset = Vector2Int.zero;
        ClearVisualMoveResult();

        RangeGridIndices = rangeGridIndices != null
            ? new List<int>(rangeGridIndices)
            : new List<int>();
        TargetGridIndices = new List<int>(RangeGridIndices);
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
        ShieldCost = 0;
        BaseHPCost = 0;
        BaseCost = 0;
        BaseResourceCost = 0;
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
            case ReferenceResource.MovePoint:
                Cost = cost;
                break;

            case ReferenceResource.UniqueResource:
                ResourceCost = cost;
                break;
        }

        BaseHPCost = HPCost;
        BaseCost = Cost;
        BaseResourceCost = ResourceCost;
        BaseShieldCost = ShieldCost;
    }

    private int GetCostValue(SkillMasterData skillData)
    {
        return skillData == null
            ? 0
            : Mathf.Max(0, skillData.ResourceCostValue);
    }
}

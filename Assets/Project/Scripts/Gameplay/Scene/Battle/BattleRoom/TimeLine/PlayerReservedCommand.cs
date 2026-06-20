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

    public int HealthCost { get; private set; }
    public int StaminaCost { get; private set; }
    public int ResourceCost { get; private set; }
    public int MoveCost { get; private set; }
    public int ShieldCost { get; private set; }

    public int BaseHealthCost { get; private set; }
    public int BaseStaminaCost { get; private set; }
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
    public int MoveDistancePerStamina { get; private set; } = 1;
    public int ExecutedMoveDistance { get; private set; } = -1;
    public bool MoveStaminaCostConsumed { get; private set; }
    public bool BlockedMoveStaminaRefundApplied { get; private set; }

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
        HealthCost = BaseHealthCost;
        StaminaCost = BaseStaminaCost;
        ResourceCost = BaseResourceCost;
        MoveCost = BaseMoveCost;
        ShieldCost = BaseShieldCost;
        ReservationCostModifiersApplied = false;
    }

    public void SetBaseCosts(
        int healthCost,
        int staminaCost,
        int resourceCost,
        int moveCost,
        int shieldCost)
    {
        BaseHealthCost = Mathf.Max(0, healthCost);
        BaseStaminaCost = Mathf.Max(0, staminaCost);
        BaseResourceCost = Mathf.Max(0, resourceCost);
        BaseMoveCost = Mathf.Max(0, moveCost);
        BaseShieldCost = Mathf.Max(0, shieldCost);

        ResetCostsToBase();
    }

    public void SetCosts(
        int healthCost,
        int staminaCost,
        int resourceCost,
        int moveCost,
        int shieldCost)
    {
        HealthCost = Mathf.Max(0, healthCost);
        StaminaCost = Mathf.Max(0, staminaCost);
        ResourceCost = Mathf.Max(0, resourceCost);
        MoveCost = Mathf.Max(0, moveCost);
        ShieldCost = Mathf.Max(0, shieldCost);
    }

    public void SetMoveReservationCost(
        int plannedMoveDistance,
        int moveDistancePerStamina)
    {
        PlannedMoveDistance = Mathf.Max(0, plannedMoveDistance);
        MoveDistancePerStamina = Mathf.Max(1, moveDistancePerStamina);

        SetBaseCosts(
            0,
            CalculateMoveStaminaCost(PlannedMoveDistance, MoveDistancePerStamina),
            0,
            0,
            0
        );
    }

    public void SetExecutedMoveDistance(int executedMoveDistance)
    {
        ExecutedMoveDistance = Mathf.Max(0, executedMoveDistance);
    }

    public void MarkMoveStaminaCostConsumed()
    {
        MoveStaminaCostConsumed = true;
    }

    public int ApplyBlockedMoveStaminaRefund()
    {
        if (BlockedMoveStaminaRefundApplied)
            return 0;

        BlockedMoveStaminaRefundApplied = true;

        int refund = GetBlockedMoveStaminaRefund();

        if (refund <= 0 || UserRuntime == null)
            return 0;

        int maxStamina = UserRuntime.MaxStamina > 0
            ? UserRuntime.MaxStamina
            : UserRuntime.CurrentStamina + refund;

        UserRuntime.CurrentStamina = Mathf.Min(
            maxStamina,
            UserRuntime.CurrentStamina + refund
        );

        return refund;
    }

    public int GetBlockedMoveStaminaRefund()
    {
        if (PlannedMoveDistance <= 0 || ExecutedMoveDistance < 0)
            return 0;

        int actualStaminaCost = CalculateMoveStaminaCost(
            ExecutedMoveDistance,
            MoveDistancePerStamina
        );

        int blockedStaminaCost = Mathf.Max(0, StaminaCost - actualStaminaCost);
        return blockedStaminaCost / 2;
    }

    public static int CalculateMoveStaminaCost(
        int moveDistance,
        int moveDistancePerStamina)
    {
        int safeDistance = Mathf.Max(0, moveDistance);

        if (safeDistance <= 0)
            return 0;

        int safeDistancePerStamina = Mathf.Max(1, moveDistancePerStamina);
        return Mathf.CeilToInt(safeDistance / (float)safeDistancePerStamina);
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
        HealthCost = 0;
        StaminaCost = 0;
        ResourceCost = 0;
        MoveCost = 0;
        ShieldCost = 0;
        BaseHealthCost = 0;
        BaseStaminaCost = 0;
        BaseResourceCost = 0;
        BaseMoveCost = 0;
        BaseShieldCost = 0;

        if (skillData == null)
            return;

        int cost = GetCostValue(skillData);

        switch (skillData.ReferenceResource)
        {
            case ReferenceResource.Health:
                HealthCost = cost;
                break;

            case ReferenceResource.Stamina:
                StaminaCost = cost;
                break;

            case ReferenceResource.UniqueResource:
                ResourceCost = cost;
                break;

            case ReferenceResource.MovePoint:
                MoveCost = cost;
                break;
        }

        BaseHealthCost = HealthCost;
        BaseStaminaCost = StaminaCost;
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
            case ReferenceResource.Health:
                return Mathf.Max(0, UserRuntime.PreviewHealth);

            case ReferenceResource.Stamina:
                return Mathf.Max(0, UserRuntime.PreviewStamina);

            case ReferenceResource.UniqueResource:
                return Mathf.Max(0, UserRuntime.PreviewResource);

            case ReferenceResource.MovePoint:
                return Mathf.Max(0, UserRuntime.PreviewMoveLevel);

            default:
                return 0;
        }
    }
}

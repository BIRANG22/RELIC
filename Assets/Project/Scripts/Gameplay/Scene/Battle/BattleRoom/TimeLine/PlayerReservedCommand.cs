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

    public Vector2Int EffectiveVisualMoveOffset =>
        VisualMoveOffset != Vector2Int.zero ? VisualMoveOffset : EffectiveMoveOffset;

    public int EffectiveVisualMoveGridIndex =>
        VisualMoveGridIndex >= 0 ? VisualMoveGridIndex : EffectiveMoveGridIndex;

    public bool SkipMoveVisual { get; private set; }

    public void SetSkipMoveVisual(bool skip)
    {
        SkipMoveVisual = skip;
    }
    public void SetVisualMoveResult(int visualGridIndex, Vector2Int visualMoveOffset)
    {
        VisualMoveGridIndex = visualGridIndex;
        VisualMoveOffset = visualMoveOffset;
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

        RangeGridIndices = rangeGridIndices != null ? new List<int>(rangeGridIndices) : new List<int>();
        TargetGridIndices = new List<int> { selectedGridIndex };
    }

    private void CalculateCosts(SkillMasterData skillData)
    {
        HealthCost = 0;
        StaminaCost = 0;
        ResourceCost = 0;
        MoveCost = 0;
        ShieldCost = 0;

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

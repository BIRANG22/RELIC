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
    public PlayerReservedCommand(CharacterRuntimeData userRuntime, SkillMasterData skillData)
    {
        UserRuntime = userRuntime;
        SkillData = skillData;

        CalculateCosts(skillData);

        Debug.Log(
            $"[PlayerReservedCommand] Skill:{SkillId} / " +
            $"Reference:{skillData?.ReferenceResource} / " +
            $"CostType:{skillData?.ResourceCostType} / " +
            $"CostValue:{skillData?.ResourceCostValue} / " +
            $"StaminaCost:{StaminaCost}"
        );
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
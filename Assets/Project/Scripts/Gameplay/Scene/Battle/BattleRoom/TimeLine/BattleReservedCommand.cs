using Relic.Gameplay.Data;
using UnityEngine;

public class BattleReservedCommand
{
    public CharacterRuntimeData UserRuntime { get; private set; }
    public SkillMasterData SkillData { get; private set; }

    public int HealthCost { get; private set; }
    public int StaminaCost { get; private set; }
    public int ResourceCost { get; private set; }
    public int MoveCost { get; private set; }
    public int ShieldCost { get; private set; }

    public string CharacterId => UserRuntime != null ? UserRuntime.CharacterId : "";
    public string SkillId => SkillData != null ? SkillData.SkillId : "";
    public string SkillName => SkillData != null ? SkillData.Name : "";

    public BattleReservedCommand(CharacterRuntimeData userRuntime, SkillMasterData skillData)
    {
        UserRuntime = userRuntime;
        SkillData = skillData;

        CalculateCosts(skillData);

        Debug.Log(
            $"[BattleReservedCommand] Skill:{SkillId} / " +
            $"Reference:{skillData?.ReferenceResource} / " +
            $"CostType:{skillData?.ResourceCostType} / " +
            $"CostValue:{skillData?.ResourceCostValue} / " +
            $"StaminaCost:{StaminaCost}"
        );
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
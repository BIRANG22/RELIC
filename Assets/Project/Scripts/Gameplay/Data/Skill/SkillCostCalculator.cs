using Relic.Gameplay.Data;
using UnityEngine;

public static class SkillCostCalculator
{
    public static int GetCurrentResource(CharacterRuntimeData caster, ReferenceResource type)
    {
        if (caster == null)
            return 0;

        return type switch
        {
            ReferenceResource.HP => caster.CurrentHP,
            ReferenceResource.Cost => caster.CurrentCost,
            ReferenceResource.UniqueResource => caster.CurrentResource,
            ReferenceResource.MovePoint => caster.CurrentCost,
            _ => 0
        };
    }

    public static int GetPreviewResource(CharacterRuntimeData caster, ReferenceResource type)
    {
        if (caster == null)
            return 0;

        return type switch
        {
            ReferenceResource.HP => caster.PreviewHP,
            ReferenceResource.Cost => caster.PreviewCost,
            ReferenceResource.UniqueResource => caster.PreviewResource,
            ReferenceResource.MovePoint => caster.PreviewCost,
            _ => 0
        };
    }

    public static bool TryGetPreviewPayAmount(
        CharacterRuntimeData caster,
        SkillMasterData skill,
        out int payAmount)
    {
        payAmount = 0;

        if (caster == null || skill == null)
            return false;

        int available = GetPreviewResource(caster, skill.ReferenceResource);
        int costValue = Mathf.Max(0, skill.ResourceCostValue);

        switch (skill.ResourceCostType)
        {
            case ResourceCostType.None:
                payAmount = 0;
                return true;

            case ResourceCostType.Fixed:
                payAmount = costValue;
                return available >= payAmount;

            case ResourceCostType.AllCurrent:
                payAmount = available;
                int minimum = BattleEquipmentEffectService.GetAllCurrentMinimumCost(
                    caster,
                    skill,
                    costValue);
                return available >= minimum;

            default:
                return false;
        }
    }
}

using Relic.Gameplay.Data;

public static class SkillCostCalculator
{
    public static int GetCurrentResource(CharacterRuntimeData caster, ReferenceResource type)
    {
        return type switch
        {
            ReferenceResource.Health => caster.CurrentHealth,
            ReferenceResource.Stamina => caster.CurrentStamina,
            ReferenceResource.UniqueResource => caster.CurrentResource,
            ReferenceResource.MovePoint => caster.CurrentMoveLevel,
            _ => 0
        };
    }

    public static bool TryGetPayAmount(
        CharacterRuntimeData caster,
        SkillMasterData skill,
        out int payAmount)
    {
        payAmount = 0;

        if (caster == null || skill == null)
            return false;

        int current = GetCurrentResource(caster, skill.ReferenceResource);

        switch (skill.ResourceCostType)
        {
            case ResourceCostType.None:
                payAmount = 0;
                return true;

            case ResourceCostType.Fixed:
                payAmount = skill.ResourceCostValue;
                return current >= payAmount;

            case ResourceCostType.AllCurrent:
                // ResourceCostValue는 최소 요구량
                payAmount = current;
                return current >= skill.ResourceCostValue;

            default:
                return false;
        }
    }

    public static bool CanUse(CharacterRuntimeData caster, SkillMasterData skill)
    {
        return TryGetPayAmount(caster, skill, out _);
    }

    public static bool TryPay(CharacterRuntimeData caster, SkillMasterData skill, out int payAmount)
    {
        if (!TryGetPayAmount(caster, skill, out payAmount))
            return false;

        switch (skill.ReferenceResource)
        {
            case ReferenceResource.Health:
                caster.CurrentHealth -= payAmount;
                break;

            case ReferenceResource.Stamina:
                caster.CurrentStamina -= payAmount;
                break;

            case ReferenceResource.UniqueResource:
                caster.CurrentResource -= payAmount;
                break;

            case ReferenceResource.MovePoint:
                caster.CurrentMoveLevel -= payAmount;
                break;
        }

        return true;
    }
}
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
        string formula = string.IsNullOrWhiteSpace(skill.ResourceCostFormula)
            ? skill.ResourceCostValue.ToString()
            : skill.ResourceCostFormula;
        if (!SkillNumericExpression.TryParse(formula, out SkillNumericExpression expression))
            return false;

        int resolvedX = expression.ResolveMaximumX(available);
        if (expression.UsesX && resolvedX <= 0)
            return false;

        payAmount = expression.Resolve(available, resolvedX);
        return available >= payAmount;
    }
}

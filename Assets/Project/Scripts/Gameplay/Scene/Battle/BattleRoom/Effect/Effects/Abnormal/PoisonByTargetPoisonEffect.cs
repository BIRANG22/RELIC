using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;

public sealed class PoisonByTargetPoisonEffect : BattleEffectBase
{
    public override string EffectId => "E_PoisonByTargetPoison";
    protected override void Apply(BattleEffectContext context)
    {
        if (context == null) return;
        if (context.Value <= 0)
        {
            Debug.LogWarning(
                "[PoisonByTargetPoisonEffect] ValueRate must be greater than zero.");
            return;
        }
        List<StatusEffectRuntimeData> statuses = context.PlayerTarget?.RuntimeData?.StatusEffects ??
            context.MonsterTarget?.RuntimeData?.StatusEffects;
        int poison = 0;
        if (statuses != null)
            for (int i = 0; i < statuses.Count; i++)
                if (statuses[i] != null && statuses[i].EffectId == "E_Poison")
                    poison = statuses[i].Stack;
        int amount = NewSkillEffectRules.ResolvePoisonByTargetPoison(poison, context.Value);
        if (amount <= 0) return;
        if (context.PlayerTarget != null)
            BattleEffectUtility.AddStatusToPlayer(context.PlayerTarget, "E_Poison", amount);
        else if (context.MonsterTarget != null)
            BattleEffectUtility.AddStatusToMonster(context.MonsterTarget, "E_Poison", amount);
    }
}

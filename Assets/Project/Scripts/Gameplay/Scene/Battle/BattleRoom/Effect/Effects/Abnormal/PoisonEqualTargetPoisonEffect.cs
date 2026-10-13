using System.Collections.Generic;
using Relic.Gameplay.Data;

public sealed class PoisonEqualTargetPoisonEffect : BattleEffectBase
{
    public override string EffectId => "E_PoisonEqualTargetPoison";

    protected override void Apply(BattleEffectContext context)
    {
        if (context == null)
            return;

        List<StatusEffectRuntimeData> statuses = context.PlayerTarget?.RuntimeData?.StatusEffects ??
            context.MonsterTarget?.RuntimeData?.StatusEffects;
        int amount = NewSkillEffectRules.ResolveEqualTargetPoison(GetPoisonStack(statuses));
        if (amount <= 0)
            return;

        if (context.PlayerTarget != null)
            BattleEffectUtility.AddStatusToPlayer(context.PlayerTarget, "E_Poison", amount);
        else if (context.MonsterTarget != null)
            BattleEffectUtility.AddStatusToMonster(context.MonsterTarget, "E_Poison", amount);
    }

    private static int GetPoisonStack(List<StatusEffectRuntimeData> statuses)
    {
        if (statuses == null)
            return 0;

        for (int i = 0; i < statuses.Count; i++)
        {
            StatusEffectRuntimeData status = statuses[i];
            if (status != null && status.EffectId == "E_Poison")
                return status.Stack;
        }

        return 0;
    }
}

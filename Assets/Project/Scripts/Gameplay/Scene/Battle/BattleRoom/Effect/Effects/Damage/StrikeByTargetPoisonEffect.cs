using System.Collections.Generic;
using Relic.Gameplay.Data;

public sealed class StrikeByTargetPoisonEffect : BattleEffectBase
{
    public override string EffectId => "E_StrikeByTargetPoison";

    protected override void Apply(BattleEffectContext context)
    {
        if (context == null)
            return;

        List<StatusEffectRuntimeData> statuses = context.PlayerTarget?.RuntimeData?.StatusEffects ??
            context.MonsterTarget?.RuntimeData?.StatusEffects;
        int poison = GetStack(statuses, "E_Poison");
        int originalValue = context.Value;
        int originalCount = context.Count;

        context.Value = NewSkillEffectRules.ResolveTargetPoisonStrikeDamage(poison);
        context.Count = 1;
        new StrikeEffect().Execute(context);

        context.Value = originalValue;
        context.Count = originalCount;
    }

    private static int GetStack(List<StatusEffectRuntimeData> statuses, string effectId)
    {
        if (statuses == null)
            return 0;

        for (int i = 0; i < statuses.Count; i++)
        {
            StatusEffectRuntimeData status = statuses[i];
            if (status != null && status.EffectId == effectId)
                return status.Stack;
        }

        return 0;
    }
}

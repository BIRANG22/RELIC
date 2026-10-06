using System.Collections.Generic;
using Relic.Gameplay.Data;

public sealed class StrikeByVulnerableEffect : BattleEffectBase
{
    public override string EffectId => "E_StrikeByVulnerable";
    protected override void Apply(BattleEffectContext context)
    {
        if (context == null) return;
        List<StatusEffectRuntimeData> statuses = context.PlayerTarget?.RuntimeData?.StatusEffects ??
            context.MonsterTarget?.RuntimeData?.StatusEffects;
        int vulnerable = 0;
        if (statuses != null)
            for (int i = 0; i < statuses.Count; i++)
                if (statuses[i] != null && statuses[i].EffectId == "E_Vulnerable")
                    vulnerable = statuses[i].Stack;
        int original = context.Value;
        context.Value = NewSkillEffectRules.ResolveVulnerableStrikeDamage(original, vulnerable, context.Count);
        context.Count = 1;
        new StrikeEffect().Execute(context);
        context.Value = original;
    }
}


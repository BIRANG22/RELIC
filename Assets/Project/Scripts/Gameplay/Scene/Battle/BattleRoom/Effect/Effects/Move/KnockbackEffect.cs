using UnityEngine;

public class KnockbackEffect : BattleEffectBase
{
    public override string EffectId => "E_Knockback";

    protected override void Apply(BattleEffectContext context)
    {
        if (context == null)
            return;

        Debug.Log($"[Effect] E_Knockback ¡ÿ∫Òµ  / Value:{context.Value} / Count:{context.Count}");
    }
}
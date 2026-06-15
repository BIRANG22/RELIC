using UnityEngine;

public class GrabEffect : BattleEffectBase
{
    public override string EffectId => "E_Grab";

    protected override void Apply(BattleEffectContext context)
    {
        if (context == null)
            return;

        Debug.Log($"[Effect] E_Grab ¡ÿ∫Òµ  / Value:{context.Value} / Count:{context.Count}");
    }
}
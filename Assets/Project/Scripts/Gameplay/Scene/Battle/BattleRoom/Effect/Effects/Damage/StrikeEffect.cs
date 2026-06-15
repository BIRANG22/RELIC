using UnityEngine;

public class StrikeEffect : BattleEffectBase
{
    public override string EffectId => "E_Strike";

    protected override void Apply(BattleEffectContext context)
    {
        if (context == null)
            return;

        if (context.PlayerTarget != null)
        {
            BattleEffectUtility.DamagePlayer(context.PlayerTarget, context.Value);
            Debug.Log($"[Effect] E_Strike Player / Damage:{context.Value}");
            return;
        }

        if (context.MonsterTarget != null)
        {
            BattleEffectUtility.DamageMonster(context.MonsterTarget, context.Value);
            Debug.Log($"[Effect] E_Strike Monster / Damage:{context.Value}");
        }
    }
}
using UnityEngine;

public class DrainEffect : BattleEffectBase
{
    public override string EffectId => "E_Drain";

    protected override void Apply(BattleEffectContext context)
    {
        if (context == null)
            return;

        int damage = Mathf.Max(0, context.Value);

        if (context.PlayerTarget != null)
        {
            BattleEffectUtility.DamagePlayer(context.PlayerTarget, damage);

            if (context.MonsterCaster != null)
                BattleEffectUtility.HealMonster(context.MonsterCaster, damage);

            Debug.Log($"[Effect] E_Drain Player / Damage:{damage}");
            return;
        }

        if (context.MonsterTarget != null)
        {
            BattleEffectUtility.DamageMonster(context.MonsterTarget, damage);

            if (context.PlayerCaster != null)
                BattleEffectUtility.HealPlayer(context.PlayerCaster, damage);

            Debug.Log($"[Effect] E_Drain Monster / Damage:{damage}");
        }
    }
}
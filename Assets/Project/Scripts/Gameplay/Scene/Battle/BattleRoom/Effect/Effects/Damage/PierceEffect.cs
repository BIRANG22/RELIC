using UnityEngine;

public class PierceEffect : BattleEffectBase
{
    public override string EffectId => "E_Pierce";

    protected override void Apply(BattleEffectContext context)
    {
        if (context == null)
            return;

        int damage = Mathf.Max(0, context.Value);

        if (context.PlayerTarget != null)
        {
            BattleEffectUtility.PierceDamagePlayer(context.PlayerTarget, damage);
        }

        if (context.MonsterTarget != null)
        {
            BattleEffectUtility.PierceDamageMonster(context.MonsterTarget, damage);
        }
    }
}

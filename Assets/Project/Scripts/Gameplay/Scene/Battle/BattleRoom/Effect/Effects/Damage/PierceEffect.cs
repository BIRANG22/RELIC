using UnityEngine;

public class PierceEffect : BattleEffectBase
{
    public override string EffectId => "E_Pierce";

    protected override void Apply(BattleEffectContext context)
    {
        if (context == null)
            return;

        int damage = Mathf.Max(0, context.Value);
        int count = Mathf.Max(1, context.Count);

        for (int i = 0; i < count; i++)
        {
            if (context.PlayerTarget != null)
            {
                BattleEffectUtility.PierceDamagePlayer(context.PlayerTarget, damage);

                if (context.PlayerTarget.RuntimeData.CurrentHealth <= 0)
                    break;
            }

            if (context.MonsterTarget != null)
            {
                BattleEffectUtility.PierceDamageMonster(context.MonsterTarget, damage);

                if (context.MonsterTarget.RuntimeData.IsDead)
                    break;
            }
        }
    }
}
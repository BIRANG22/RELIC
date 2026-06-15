using UnityEngine;

public class StrikeEffect : BattleEffectBase
{
    public override string EffectId => "E_Strike";

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
                BattleEffectUtility.DamagePlayer(context.PlayerTarget, damage);

                if (context.PlayerTarget.RuntimeData.CurrentHealth <= 0)
                    break;
            }

            if (context.MonsterTarget != null)
            {
                BattleEffectUtility.DamageMonster(context.MonsterTarget, damage);

                if (context.MonsterTarget.RuntimeData.IsDead)
                    break;
            }
        }
    }
}
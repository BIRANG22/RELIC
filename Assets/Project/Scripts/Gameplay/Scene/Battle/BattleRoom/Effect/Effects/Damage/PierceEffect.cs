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
            int finalDamage = BattleDamageModifierUtility.CalculateFinalDamageToPlayer(context, damage);

            BattleEffectUtility.PierceDamagePlayer(context.PlayerTarget, finalDamage);
        }

        if (context.MonsterTarget != null)
        {
            int finalDamage = BattleDamageModifierUtility.CalculateFinalDamageToMonster(context, damage);

            int dealtDamage = BattleEffectUtility.PierceDamageMonster(context.MonsterTarget, finalDamage);

            if (dealtDamage > 0 && context.PlayerCaster != null)
                BattleEffectUtility.OnPlayerDamagedEnemy?.Invoke(context.PlayerCaster);
        }
    }
}

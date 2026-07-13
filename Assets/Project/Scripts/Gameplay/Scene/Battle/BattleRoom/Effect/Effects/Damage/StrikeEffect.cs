using UnityEngine;

public class StrikeEffect : BattleEffectBase
{
    public override string EffectId => "E_Strike";

    protected override void Apply(BattleEffectContext context)
    {
        if (context == null)
            return;

        int damage = Mathf.Max(0, context.Value);

        if (context.PlayerTarget != null)
        {
            int finalDamage = CalculateFinalDamageToPlayer(context, damage);

            BattleEffectUtility.DamagePlayer(context.PlayerTarget, finalDamage);
        }

        if (context.MonsterTarget != null)
        {
            int finalDamage = CalculateFinalDamageToMonster(context, damage);

            int dealtDamage = BattleEffectUtility.DamageMonster(context.MonsterTarget, finalDamage);

            if (dealtDamage > 0 && context.PlayerCaster != null)
                BattleEffectUtility.OnPlayerDamagedEnemy?.Invoke(context.PlayerCaster);
        }
    }

    private int CalculateFinalDamageToPlayer(BattleEffectContext context, int baseDamage)
    {
        return BattleDamageModifierUtility.CalculateFinalDamageToPlayer(context, baseDamage);
    }

    private int CalculateFinalDamageToMonster(BattleEffectContext context, int baseDamage)
    {
        return BattleDamageModifierUtility.CalculateFinalDamageToMonster(context, baseDamage);
    }
}

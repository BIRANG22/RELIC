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
            bool wasAlive = !context.MonsterTarget.RuntimeData.IsDead;
            int finalDamage = BattleDamageModifierUtility.CalculateFinalDamageToMonster(context, damage);

            int dealtDamage = BattleEffectUtility.PierceDamageMonster(context.MonsterTarget, finalDamage);

            if (dealtDamage > 0 && context.PlayerCaster != null)
            {
                BattleRunStatisticsRecorder.RecordDamageDealt(
                    context.PlayerCaster.RuntimeData.CharacterId,
                    dealtDamage,
                    wasAlive && context.MonsterTarget.RuntimeData.IsDead);
                BattleEffectUtility.OnPlayerDamagedEnemy?.Invoke(context.PlayerCaster);
            }
        }
    }
}

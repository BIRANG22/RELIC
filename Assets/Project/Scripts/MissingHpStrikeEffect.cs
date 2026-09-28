using UnityEngine;

public class MissingHpStrikeEffect : BattleEffectBase
{
    public override string EffectId => "E_MissingHPStrike";

    protected override void Apply(BattleEffectContext context)
    {
        if (context == null)
            return;

        int damage = Mathf.Max(0, context.Value);

        if (context.PlayerTarget != null)
        {
            int finalDamage = BattleDamageModifierUtility.CalculateFinalDamageToPlayer(context, damage);
            BattleEffectUtility.DamagePlayer(
                context.PlayerTarget,
                finalDamage,
                context.MonsterCaster != null || context.MonsterCommand != null);
        }

        if (context.MonsterTarget != null)
        {
            bool wasAlive = !context.MonsterTarget.RuntimeData.IsDead;
            int finalDamage = BattleDamageModifierUtility.CalculateFinalDamageToMonster(context, damage);
            int dealtDamage = BattleEffectUtility.DamageMonster(context.MonsterTarget, finalDamage);

            BattleEquipmentEffectService.ApplyPlayerDamageDealtEffects(
                context,
                dealtDamage,
                wasAlive && context.MonsterTarget.RuntimeData.IsDead);

            BattleEquipmentEffectService.ApplyPlayerLifesteal(context, dealtDamage);

            bool killedTarget = wasAlive && context.MonsterTarget.RuntimeData.IsDead;
            if (dealtDamage > 0 && context.PlayerCaster != null)
            {
                BattleRunStatisticsRecorder.RecordDamageDealt(
                    context.PlayerCaster.RuntimeData.CharacterId,
                    dealtDamage,
                    killedTarget);
            }

            if (killedTarget && context.PlayerCaster != null)
            {
                int healAmount = BattleEquipmentEffectService.GetKillHealAmount(
                    context.PlayerCaster.RuntimeData);
                if (healAmount > 0)
                    BattleEffectUtility.HealPlayer(context.PlayerCaster, healAmount);
            }
        }
    }
}

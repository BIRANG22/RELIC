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

            BattleEffectUtility.PierceDamagePlayer(context.PlayerTarget, finalDamage, context.MonsterCaster != null || context.MonsterCommand != null);
        }

        if (context.MonsterTarget != null)
        {
            bool wasAlive = !context.MonsterTarget.RuntimeData.IsDead;
            int finalDamage = BattleDamageModifierUtility.CalculateFinalDamageToMonster(context, damage);

            int dealtDamage = BattleEffectUtility.PierceDamageMonster(context.MonsterTarget, finalDamage);
            bool killedByPierce = wasAlive && context.MonsterTarget.RuntimeData.IsDead;
            if (context.ExecutionResult != null)
            {
                context.ExecutionResult.RecordImpact(new SkillImpactResult
                {
                    TargetRuntimeId = context.MonsterTarget.RuntimeData.RuntimeId,
                    TargetGridIndex = context.MonsterTarget.MainGridIndex,
                    Attempted = true,
                    Hit = dealtDamage > 0,
                    DamageAmount = Mathf.Max(0, dealtDamage),
                    Killed = killedByPierce
                });
            }

            BattleEquipmentEffectService.ApplyPlayerDamageDealtEffects(
                context,
                dealtDamage,
                wasAlive && context.MonsterTarget.RuntimeData.IsDead);

            BattleEquipmentEffectService.ApplyPlayerLifesteal(
                context,
                dealtDamage);

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

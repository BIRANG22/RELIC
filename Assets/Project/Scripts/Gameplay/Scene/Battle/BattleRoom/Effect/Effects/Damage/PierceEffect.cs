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
            bool wasAlive = !context.PlayerTarget.RuntimeData.IsDead;
            int previousHp = context.PlayerTarget.RuntimeData.CurrentHP;
            int wardDamage = BattleWardReactionService.ResolveReactionDamage(
                context.PlayerTarget.RuntimeData.StatusEffects,
                1);
            int finalDamage = BattleDamageModifierUtility.CalculateFinalDamageToPlayer(context, damage);

            BattleEffectUtility.PierceDamagePlayer(context.PlayerTarget, finalDamage, context.MonsterCaster != null || context.MonsterCommand != null);
            int dealtDamage = Mathf.Max(0, previousHp - context.PlayerTarget.RuntimeData.CurrentHP);
            BattleWardReactionService.Apply(context, dealtDamage, wardDamage);
            BattleDamageModifierUtility.TryConsumeVulnerableOnDamageTrigger(
                context.PlayerTarget.RuntimeData.StatusEffects,
                damage,
                wasAlive);
        }

        if (context.MonsterTarget != null)
        {
            bool wasAlive = !context.MonsterTarget.RuntimeData.IsDead;
            int wardDamage = BattleWardReactionService.ResolveReactionDamage(
                context.MonsterTarget.RuntimeData.StatusEffects,
                1);
            int finalDamage = BattleDamageModifierUtility.CalculateFinalDamageToMonster(context, damage);

            int dealtDamage = BattleEffectUtility.PierceDamageMonster(context.MonsterTarget, finalDamage);
            BattleWardReactionService.Apply(context, dealtDamage, wardDamage);
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

            BattleDamageModifierUtility.TryConsumeVulnerableOnDamageTrigger(
                context.MonsterTarget.RuntimeData.StatusEffects,
                damage,
                wasAlive);
        }
    }
}

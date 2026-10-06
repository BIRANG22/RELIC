using UnityEngine;

public class StrikeEffect : BattleEffectBase
{
    public override string EffectId => "E_Strike";

    protected override void Apply(BattleEffectContext context)
    {
        if (context == null)
            return;

        int damage = Mathf.Max(0, context.Value);
        if (HasSkillEffect(context, "E_DamageUpIfBleeding"))
        {
            var targetStatuses = context.PlayerTarget?.RuntimeData?.StatusEffects ??
                context.MonsterTarget?.RuntimeData?.StatusEffects;
            damage = NewSkillEffectRules.ApplyBleedingDamageBonus(
                damage,
                GetStack(targetStatuses, "E_Bleed") > 0);
        }

        if (context.PlayerTarget != null)
        {
            bool wasAlive = !context.PlayerTarget.RuntimeData.IsDead;
            int previousHp = context.PlayerTarget.RuntimeData.CurrentHP;
            int finalDamage = CalculateFinalDamageToPlayer(context, damage);
            BattleEffectUtility.DamagePlayer(context.PlayerTarget, finalDamage, context.MonsterCaster != null || context.MonsterCommand != null);
            int dealtDamage = Mathf.Max(0, previousHp - context.PlayerTarget.RuntimeData.CurrentHP);
            RecordImpact(
                context,
                context.PlayerTarget.RuntimeData.CharacterId,
                context.PlayerTarget.CurrentGridIndex,
                dealtDamage,
                wasAlive && context.PlayerTarget.RuntimeData.IsDead,
                context.PlayerTarget.RuntimeData.StatusEffects);
        }

        if (context.MonsterTarget != null)
        {
            bool wasAlive = !context.MonsterTarget.RuntimeData.IsDead;
            int finalDamage = CalculateFinalDamageToMonster(context, damage);

            int dealtDamage = BattleEffectUtility.DamageMonster(context.MonsterTarget, finalDamage);

            RecordImpact(
                context,
                context.MonsterTarget.RuntimeData.RuntimeId,
                context.MonsterTarget.MainGridIndex,
                dealtDamage,
                wasAlive && context.MonsterTarget.RuntimeData.IsDead,
                context.MonsterTarget.RuntimeData.StatusEffects);

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

    private int CalculateFinalDamageToPlayer(BattleEffectContext context, int baseDamage)
    {
        return BattleDamageModifierUtility.CalculateFinalDamageToPlayer(context, baseDamage);
    }

    private int CalculateFinalDamageToMonster(BattleEffectContext context, int baseDamage)
    {
        return BattleDamageModifierUtility.CalculateFinalDamageToMonster(context, baseDamage);
    }

    private static void RecordImpact(
        BattleEffectContext context,
        string targetRuntimeId,
        int targetGridIndex,
        int damageAmount,
        bool killed,
        System.Collections.Generic.List<Relic.Gameplay.Data.StatusEffectRuntimeData> statuses)
    {
        if (context.ExecutionResult == null)
            return;

        context.ExecutionResult.RecordImpact(new SkillImpactResult
        {
            TargetRuntimeId = targetRuntimeId,
            TargetGridIndex = targetGridIndex,
            Attempted = true,
            Hit = damageAmount > 0,
            DamageAmount = Mathf.Max(0, damageAmount),
            Killed = killed,
            HadBleedingBeforeHit = GetStack(statuses, "E_Bleed") > 0,
            VulnerableValueBeforeHit = GetStack(statuses, "E_Vulnerable"),
            PoisonValueBeforeEffect = GetStack(statuses, "E_Poison")
        });
    }

    private static int GetStack(
        System.Collections.Generic.List<Relic.Gameplay.Data.StatusEffectRuntimeData> statuses,
        string effectId)
    {
        if (statuses == null)
            return 0;
        for (int i = 0; i < statuses.Count; i++)
        {
            if (statuses[i] != null && statuses[i].EffectId == effectId)
                return Mathf.Max(0, statuses[i].Stack);
        }
        return 0;
    }

    private static bool HasSkillEffect(BattleEffectContext context, string effectId)
    {
        var entries = context?.PlayerSkillData?.EffectEntries;
        if (entries == null)
            return false;
        for (int i = 0; i < entries.Count; i++)
            if (entries[i] != null && entries[i].EffectId == effectId)
                return true;
        return false;
    }
}

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
                int finalDamage = CalculateFinalDamageToPlayer(context, damage);

                BattleEffectUtility.DamagePlayer(context.PlayerTarget, finalDamage);

                if (context.PlayerTarget.RuntimeData.CurrentHealth <= 0)
                    break;
            }

            if (context.MonsterTarget != null)
            {
                int finalDamage = CalculateFinalDamageToMonster(context, damage);

                BattleEffectUtility.DamageMonster(context.MonsterTarget, finalDamage);

                if (context.MonsterTarget.RuntimeData.IsDead)
                    break;
            }
        }
    }

    private int CalculateFinalDamageToPlayer(BattleEffectContext context, int baseDamage)
    {
        float damage = baseDamage;

        if (context.PlayerCaster != null)
            damage = ApplyAttackerModifiers(damage, context.PlayerCaster.RuntimeData.StatusEffects);

        if (context.MonsterCaster != null)
            damage = ApplyAttackerModifiers(damage, context.MonsterCaster.RuntimeData.StatusEffects);

        if (context.PlayerTarget != null)
            damage = ApplyTargetModifiers(damage, context.PlayerTarget.RuntimeData.StatusEffects);

        return Mathf.Max(1, Mathf.CeilToInt(damage));
    }

    private int CalculateFinalDamageToMonster(BattleEffectContext context, int baseDamage)
    {
        float damage = baseDamage;

        if (context.PlayerCaster != null)
            damage = ApplyAttackerModifiers(damage, context.PlayerCaster.RuntimeData.StatusEffects);

        if (context.MonsterCaster != null)
            damage = ApplyAttackerModifiers(damage, context.MonsterCaster.RuntimeData.StatusEffects);

        if (context.MonsterTarget != null)
            damage = ApplyTargetModifiers(damage, context.MonsterTarget.RuntimeData.StatusEffects);

        return Mathf.Max(1, Mathf.CeilToInt(damage));
    }

    private float ApplyAttackerModifiers(
        float damage,
        System.Collections.Generic.List<Relic.Gameplay.Data.StatusEffectRuntimeData> statuses)
    {
        if (GetStatusStack(statuses, "E_Weaken") > 0)
            damage *= 0.7f;

        return damage;
    }

    private float ApplyTargetModifiers(
        float damage,
        System.Collections.Generic.List<Relic.Gameplay.Data.StatusEffectRuntimeData> statuses)
    {
        if (GetStatusStack(statuses, "E_Vulnerable") > 0)
            damage *= 1.5f;

        return damage;
    }

    private int GetStatusStack(
        System.Collections.Generic.List<Relic.Gameplay.Data.StatusEffectRuntimeData> statuses,
        string effectId)
    {
        if (statuses == null)
            return 0;

        for (int i = 0; i < statuses.Count; i++)
        {
            if (statuses[i] == null)
                continue;

            if (statuses[i].EffectId == effectId)
                return statuses[i].Stack;
        }

        return 0;
    }
}
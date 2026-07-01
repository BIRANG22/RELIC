using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;

public static class BattleDamageModifierUtility
{
    private const string WeakenEffectId = "E_Weaken";
    private const string CorrosionEffectId = "E_Corrosion";
    private const string VulnerableEffectId = "E_Vulnerable";
    private const string GrudgeEffectId = "E_Grudge";

    public static int CalculateFinalDamageToPlayer(BattleEffectContext context, int baseDamage)
    {
        float damage = Mathf.Max(0, baseDamage);

        if (context?.PlayerCaster != null)
            damage = ApplyAttackerModifiers(damage, context.PlayerCaster.RuntimeData.StatusEffects);

        if (context?.MonsterCaster != null)
            damage = ApplyAttackerModifiers(damage, context.MonsterCaster.RuntimeData.StatusEffects);

        if (context?.PlayerTarget != null)
            damage = ApplyTargetModifiers(damage, context.PlayerTarget.RuntimeData.StatusEffects);

        return Mathf.Max(1, Mathf.CeilToInt(damage));
    }

    public static int CalculateFinalDamageToMonster(BattleEffectContext context, int baseDamage)
    {
        float damage = Mathf.Max(0, baseDamage);

        if (context?.PlayerCaster != null)
            damage = ApplyAttackerModifiers(damage, context.PlayerCaster.RuntimeData.StatusEffects);

        if (context?.MonsterCaster != null)
            damage = ApplyAttackerModifiers(damage, context.MonsterCaster.RuntimeData.StatusEffects);

        if (context?.MonsterTarget != null)
            damage = ApplyTargetModifiers(damage, context.MonsterTarget.RuntimeData.StatusEffects);

        return Mathf.Max(1, Mathf.CeilToInt(damage));
    }

    private static float ApplyAttackerModifiers(
        float damage,
        List<StatusEffectRuntimeData> statuses)
    {
        if (GetStatusStack(statuses, WeakenEffectId) > 0)
            damage *= 0.7f;

        int corrosionStack = GetStatusStack(statuses, CorrosionEffectId);
        if (corrosionStack > 0)
            damage += corrosionStack;

        return damage;
    }

    private static float ApplyTargetModifiers(
        float damage,
        List<StatusEffectRuntimeData> statuses)
    {
        if (GetStatusStack(statuses, VulnerableEffectId) > 0)
            damage *= 1.5f;

        int grudgeStack = GetStatusStack(statuses, GrudgeEffectId);
        if (grudgeStack > 0)
            damage += grudgeStack;

        return damage;
    }

    private static int GetStatusStack(
        List<StatusEffectRuntimeData> statuses,
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

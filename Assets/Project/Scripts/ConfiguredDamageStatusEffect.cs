using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;

// Any Effect row with a configured damage modifier can use the common status handler.
public sealed class ConfiguredDamageStatusEffect : BattleEffectBase
{
    private readonly string effectId;
    public ConfiguredDamageStatusEffect(string effectId) { this.effectId = effectId; }
    public override string EffectId => effectId;
    protected override void Apply(BattleEffectContext context)
    {
        BattleEffectUtility.AddStatusToDefaultTarget(context, EffectId);
    }
}

public static class ConfiguredDamageRules
{
    public static EffectMasterData GetDefinition(StatusEffectRuntimeData status)
    {
        if (status == null || status.Stack <= 0) return null;
        return DataManager.Instance?.EffectDatabase?.Get(status.EffectId);
    }

    public static bool TryGetModifier(StatusEffectRuntimeData status, DamageModifierType type, out int value)
    {
        var data = GetDefinition(status);
        value = data != null ? data.DamageModifierValue : 0;
        return data != null && data.DamageModifier == type;
    }

    public static float ApplyPercent(float damage, int percent)
    {
        return Mathf.Max(0f, damage * (1f + percent / 100f));
    }

    public static float ApplyModifiers(float damage, List<StatusEffectRuntimeData> statuses, DamageModifierType type)
    {
        if (statuses == null) return damage;
        foreach (var status in statuses)
            if (TryGetModifier(status, type, out int value))
                damage = ApplyPercent(damage, value);
        return damage;
    }

    // Apply once at the final entry to each damage pipeline, before shields.
    public static int ApplyIncoming(int damage, List<StatusEffectRuntimeData> statuses)
    {
        if (damage <= 0 || statuses == null) return Mathf.Max(0, damage);
        float result = ApplyModifiers(damage, statuses, DamageModifierType.IncomingPercent);
        int fixedDamage = int.MaxValue;
        foreach (var status in statuses)
            if (TryGetModifier(status, DamageModifierType.FixedIncomingDamage, out int value))
                fixedDamage = Mathf.Min(fixedDamage, Mathf.Max(0, value));
        return fixedDamage != int.MaxValue ? fixedDamage : Mathf.CeilToInt(result);
    }

    public static bool IsRearAttack(BattleEffectContext context)
    {
        if (context == null) return false;
        if (context.RearAttackQualifiedAtActionStart.HasValue)
            return context.RearAttackQualifiedAtActionStart.Value;
        if (context.PlayerCaster != null && context.MonsterTarget != null)
            return BattleDamageModifierUtility.IsAttackingMonsterFromBehind(context.PlayerCaster, context.MonsterTarget);
        if (context.MonsterCaster != null && context.PlayerTarget != null)
            return BattleDamageModifierUtility.IsAttackingPlayerFromBehind(context.MonsterCaster, context.PlayerTarget);
        return false;
    }

    public static void ConsumeRearHit(BattleEffectContext context, int dealtDamage)
    {
        if (dealtDamage <= 0 || !IsRearAttack(context)) return;
        var statuses = context.PlayerCaster != null ? context.PlayerCaster.RuntimeData?.StatusEffects : context.MonsterCaster?.RuntimeData?.StatusEffects;
        if (statuses == null) return;
        bool changed = false;
        for (int i = statuses.Count - 1; i >= 0; i--)
        {
            var status = statuses[i];
            var data = GetDefinition(status);
            if (data == null || data.DamageModifier != DamageModifierType.RearOutgoingPercent ||
                data.EndTurn != EndTurn.DecreaseOnTrigger || status.IsPassive) continue;
            status.Stack--;
            if (status.Stack <= 0) statuses.RemoveAt(i);
            changed = true;
        }
        if (changed && context.PlayerCaster != null)
            BattleEffectUtility.OnPlayerHudRefreshRequested?.Invoke(context.PlayerCaster);
    }
}

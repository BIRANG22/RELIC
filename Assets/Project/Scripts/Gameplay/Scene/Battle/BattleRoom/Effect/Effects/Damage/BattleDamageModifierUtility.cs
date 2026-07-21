using System.Collections.Generic;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;

public static class BattleDamageModifierUtility
{
    private const string WeakenEffectId = "E_Weaken";
    private const string CorrosionEffectId = "E_Corrosion";
    private const string VulnerableEffectId = "E_Vulnerable";
    private const string GrudgeEffectId = "E_Grudge";
    private const string FlankEffectId = "E_Flank";
    private const string MoveFirstAttackPowerEffectId = "E_Move_First_Attack_Power";
    private const string LowHpPowerEffectId = "E_Low_HP_Power";
    private const string ActiveDamageBoostEffectId = ActiveRelicEffectIds.DamageBoostThisTurn;
    private const string ActiveDamageReductionEffectId = ActiveRelicEffectIds.DamageReductionThisTurn;

    public static int CalculateFinalDamageToPlayer(BattleEffectContext context, int baseDamage)
    {
        float damage = Mathf.Max(0, baseDamage);

        if (context?.PlayerCaster != null)
            damage = ApplyPlayerAttackerModifiers(damage, context.PlayerCaster.RuntimeData);

        if (context?.MonsterCaster != null)
            damage = ApplyMonsterAttackerModifiers(damage, context.MonsterCaster.RuntimeData);

        if (context?.MonsterCaster != null &&
            context.PlayerTarget != null &&
            HasStatus(context.MonsterCaster.RuntimeData.StatusEffects, FlankEffectId) &&
            IsAttackingPlayerFromBehind(context.MonsterCaster, context.PlayerTarget))
        {
            damage *= 1.5f;
        }

        if (context?.PlayerTarget != null)
            damage = ApplyTargetModifiers(damage, context.PlayerTarget.RuntimeData.StatusEffects);

        return Mathf.Max(1, Mathf.CeilToInt(damage));
    }

    public static int CalculateFinalDamageToMonster(BattleEffectContext context, int baseDamage)
    {
        float damage = Mathf.Max(0, baseDamage);

        if (context?.PlayerCaster != null)
            damage = ApplyPlayerAttackerModifiers(damage, context.PlayerCaster.RuntimeData);

        if (context?.MonsterCaster != null)
            damage = ApplyMonsterAttackerModifiers(damage, context.MonsterCaster.RuntimeData);

        if (context?.MonsterTarget != null)
            damage = ApplyTargetModifiers(damage, context.MonsterTarget.RuntimeData.StatusEffects);

        return Mathf.Max(1, Mathf.CeilToInt(damage));
    }

    private static float ApplyPlayerAttackerModifiers(
        float damage,
        CharacterRuntimeData runtime)
    {
        if (runtime == null)
            return damage;

        return ApplyAttackerModifiers(
            damage,
            runtime.StatusEffects,
            runtime.CurrentHP,
            runtime.MaxHP,
            BattleEquipmentEffectService.IsMoveFirstAttackPowerReady(runtime));
    }

    private static float ApplyMonsterAttackerModifiers(
        float damage,
        MonsterRuntimeData runtime)
    {
        if (runtime == null)
            return damage;

        return ApplyAttackerModifiers(
            damage,
            runtime.StatusEffects,
            runtime.CurrentHP,
            runtime.MaxHP,
            false);
    }

    private static float ApplyAttackerModifiers(
        float damage,
        List<StatusEffectRuntimeData> statuses,
        int currentHP,
        int maxHP,
        bool isMoveFirstAttackReady)
    {
        if (GetStatusStack(statuses, WeakenEffectId) > 0)
            damage *= 0.85f;

        if (GetStatusStack(statuses, ActiveDamageBoostEffectId) > 0)
            damage *= 2f;

        if (isMoveFirstAttackReady &&
            GetStatusStack(statuses, MoveFirstAttackPowerEffectId) > 0)
        {
            damage *= 1.2f;
        }

        int lowHpPowerStack = GetStatusStack(statuses, LowHpPowerEffectId);
        if (lowHpPowerStack > 0 && maxHP > 0)
        {
            float hpRatio = Mathf.Clamp01(currentHP / (float)maxHP);
            float missingHpRatio = 1f - hpRatio;
            damage *= 1f + (missingHpRatio * lowHpPowerStack * 0.01f);
        }

        int grudgeStack = GetStatusStack(statuses, GrudgeEffectId);
        if (grudgeStack > 0)
            damage += grudgeStack;

        return damage;
    }

    private static float ApplyTargetModifiers(
        float damage,
        List<StatusEffectRuntimeData> statuses)
    {
        if (GetStatusStack(statuses, VulnerableEffectId) > 0)
            damage *= 1.3f;

        if (GetStatusStack(statuses, ActiveDamageReductionEffectId) > 0)
            damage *= 0.5f;

        int corrosionStack = GetStatusStack(statuses, CorrosionEffectId);
        if (corrosionStack > 0)
            damage += corrosionStack;

        return damage;
    }


    private static bool IsAttackingPlayerFromBehind(
        MonsterUnit attacker,
        BattleCharacter target)
    {
        if (attacker == null ||
            attacker.RuntimeData == null ||
            target == null ||
            target.RuntimeData == null ||
            attacker.MainGridIndex < 0 ||
            target.CurrentGridIndex < 0)
        {
            return false;
        }

        GridManager gridManager = Object.FindFirstObjectByType<GridManager>();

        if (gridManager == null)
            return false;

        Vector2Int attackerCoord = gridManager.IndexToCoord(attacker.MainGridIndex);
        Vector2Int targetCoord = gridManager.IndexToCoord(target.CurrentGridIndex);

        if (attackerCoord.x == targetCoord.x)
            return false;

        return target.RuntimeData.Direction == BattleDirection.Right
            ? attackerCoord.x < targetCoord.x
            : attackerCoord.x > targetCoord.x;
    }

    private static bool HasStatus(
        List<StatusEffectRuntimeData> statuses,
        string effectId)
    {
        return GetStatusStack(statuses, effectId) > 0;
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

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
    private const string TargetOutgoingDamageReductionEffectId =
        ActiveRelicEffectIds.TargetOutgoingDamageReductionThisTurn;

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
        {
            damage = ApplyTargetModifiers(damage, context.PlayerTarget.RuntimeData.StatusEffects);
            damage = BattleEquipmentEffectService.ModifyIncomingDamageToPlayer(
                context.PlayerTarget.RuntimeData,
                damage);
        }

        return Mathf.Max(1, Mathf.CeilToInt(damage));
    }

    public static int CalculateFinalDamageToMonster(BattleEffectContext context, int baseDamage)
    {
        float damage = Mathf.Max(0, baseDamage);

        // 플레이어 유물의 대상 조건 보정을 먼저 적용하고,
        // 그 다음 전투 중 획득한 상태 효과를 부여 순서대로 계산합니다.
        if (context?.PlayerCaster != null)
        {
            damage = BattleEquipmentEffectService.ModifyPlayerDamageToMonster(context, damage);
            damage = ApplyPlayerAttackerModifiers(damage, context.PlayerCaster.RuntimeData);
        }

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

        return ApplyPlayerAttackerModifiersInStatusOrderFloat(
            damage,
            runtime,
            true);
    }

    public static int ApplyPlayerAttackerModifiersInStatusOrder(
        int baseValue,
        CharacterRuntimeData runtime,
        bool isAttackSkill,
        int smiteAttacksAlreadyReserved = 0)
    {
        float value = ApplyPlayerAttackerModifiersInStatusOrderFloat(
            Mathf.Max(0, baseValue),
            runtime,
            isAttackSkill,
            smiteAttacksAlreadyReserved);
        return Mathf.Max(0, Mathf.FloorToInt(value));
    }

    private static float ApplyPlayerAttackerModifiersInStatusOrderFloat(
        float damage,
        CharacterRuntimeData runtime,
        bool isAttackSkill,
        int smiteAttacksAlreadyReserved = 0)
    {
        if (runtime == null || runtime.StatusEffects == null)
            return damage;

        float value = damage;

        // StatusEffects 리스트는 효과가 처음 부여된 순서를 유지합니다.
        // 따라서 전투 중 획득한 버프/디버프는 이 순서 그대로 계산합니다.
        for (int i = 0; i < runtime.StatusEffects.Count; i++)
        {
            StatusEffectRuntimeData status = runtime.StatusEffects[i];
            if (status == null || status.Stack <= 0)
                continue;

            switch (status.EffectId)
            {
                case "E_Boost":
                    value += status.Stack;
                    break;

                case "E_Smite":
                    if (isAttackSkill && smiteAttacksAlreadyReserved < status.Stack)
                        value *= 1.5f;
                    break;

                case WeakenEffectId:
                    value *= 0.85f;
                    break;

                case ActiveDamageBoostEffectId:
                    value *= 2f;
                    break;

                case TargetOutgoingDamageReductionEffectId:
                    value *= 0.5f;
                    break;

                case MoveFirstAttackPowerEffectId:
                    if (BattleEquipmentEffectService.IsMoveFirstAttackPowerReady(runtime))
                        value *= 1.2f;
                    break;

                case LowHpPowerEffectId:
                    if (runtime.MaxHP > 0)
                    {
                        float hpRatio = Mathf.Clamp01(runtime.CurrentHP / (float)runtime.MaxHP);
                        float missingHpRatio = 1f - hpRatio;
                        value *= 1f + (missingHpRatio * status.Stack * 0.01f);
                    }
                    break;

                case GrudgeEffectId:
                    value += status.Stack;
                    break;
            }
        }

        return value;
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

        if (GetStatusStack(statuses, TargetOutgoingDamageReductionEffectId) > 0)
            damage *= 0.5f;

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

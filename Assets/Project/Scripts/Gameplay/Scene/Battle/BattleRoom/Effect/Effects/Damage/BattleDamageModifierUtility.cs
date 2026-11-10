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
    private const string BlindsideEffectId = "E_Blindside";
    private const string AmbushEffectId = "E_Ambush";
    private const string GritEffectId = "E_Grit";
    private const string MoveFirstAttackPowerEffectId = "E_Move_First_Attack_Power";
    private const string LowHpPowerEffectId = "E_Low_HP_Power";
    private const string ActiveDamageBoostEffectId = ActiveRelicEffectIds.DamageBoostThisTurn;
    private const string ActiveDamageReductionEffectId = ActiveRelicEffectIds.DamageReductionThisTurn;
    private const string TargetOutgoingDamageReductionEffectId =
        ActiveRelicEffectIds.Fatigue;

    public static bool TryConsumeVulnerableOnDamageTrigger(
        List<StatusEffectRuntimeData> statuses,
        int baseDamage,
        bool targetWasAlive)
    {
        if (!targetWasAlive || baseDamage <= 0 || statuses == null)
            return false;

        for (int i = statuses.Count - 1; i >= 0; i--)
        {
            StatusEffectRuntimeData status = statuses[i];
            if (status == null ||
                !string.Equals(status.EffectId, VulnerableEffectId, System.StringComparison.Ordinal) ||
                status.Stack <= 0)
            {
                continue;
            }

            status.Stack--;
            if (status.Stack <= 0)
                statuses.RemoveAt(i);
            return true;
        }

        return false;
    }

    public static int CalculateFinalDamageToPlayer(BattleEffectContext context, int baseDamage)
    {
        float damage = Mathf.Max(0, baseDamage);

        if (context?.PlayerCaster != null)
            damage = ApplyPlayerAttackerModifiers(damage, context.PlayerCaster.RuntimeData);

        if (context?.MonsterCaster != null)
            damage = ApplyMonsterAttackerModifiers(damage, context.MonsterCaster.RuntimeData);

        if (context?.MonsterCaster != null &&
            context.PlayerTarget != null &&
            HasStatus(context.MonsterCaster.RuntimeData.StatusEffects, BlindsideEffectId) &&
            ResolveRearAttackQualification(
                context.RearAttackQualifiedAtActionStart,
                IsAttackingPlayerFromBehind(context.MonsterCaster, context.PlayerTarget)))
        {
            damage *= 1.5f;
        }

        // Monster passive: rear attacks gain 100% damage while E_Ambush is active.
        if (context?.MonsterCaster != null &&
            context.PlayerTarget != null &&
            HasStatus(context.MonsterCaster.RuntimeData.StatusEffects, AmbushEffectId) &&
            ResolveRearAttackQualification(
                context.RearAttackQualifiedAtActionStart,
                IsAttackingPlayerFromBehind(context.MonsterCaster, context.PlayerTarget)))
        {
            damage *= 2f;
        }

        if (context?.PlayerTarget != null)
        {
            damage = ApplyTargetModifiers(damage, context.PlayerTarget.RuntimeData.StatusEffects, context.PlayerCaster?.RuntimeData);
            damage = BattleEquipmentEffectService.ModifyIncomingDamageToPlayer(
                context.PlayerTarget.RuntimeData,
                damage);
        }

        return Mathf.Max(0, Mathf.CeilToInt(damage));
    }

    public static int CalculateFinalDamageToMonster(BattleEffectContext context, int baseDamage)
    {
        float damage = Mathf.Max(0, baseDamage);

        // �÷��̾� ������ ��� ���� ������ ���� �����ϰ�,
        // �� ���� ���� �� ȹ���� ���� ȿ���� �ο� ������� ����մϴ�.
        if (context?.PlayerCaster != null)
        {
            damage = BattleEquipmentEffectService.ModifyPlayerDamageToMonster(context, damage);
            damage = ApplyPlayerAttackerModifiers(damage, context.PlayerCaster.RuntimeData);
        }

        if (context?.MonsterCaster != null)
            damage = ApplyMonsterAttackerModifiers(damage, context.MonsterCaster.RuntimeData);

        if (context?.PlayerCaster != null &&
            context.MonsterTarget != null &&
            HasStatus(context.PlayerCaster.RuntimeData.StatusEffects, BlindsideEffectId) &&
            ResolveRearAttackQualification(
                context.RearAttackQualifiedAtActionStart,
                IsAttackingMonsterFromBehind(context.PlayerCaster, context.MonsterTarget)))
        {
            damage *= 1.5f;
        }

        if (context?.MonsterTarget != null)
            damage = ApplyTargetModifiers(damage, context.MonsterTarget.RuntimeData.StatusEffects, context.PlayerCaster?.RuntimeData);

        return Mathf.Max(0, Mathf.CeilToInt(damage));
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
        return Mathf.Max(0, Mathf.CeilToInt(value));
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

        // StatusEffects ����Ʈ�� ȿ���� ó�� �ο��� ������ �����մϴ�.
        // ���� ���� �� ȹ���� ����/������� �� ���� �״�� ����մϴ�.
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
                    value = RuneEffectRules.ApplyWeaken(value);
                    break;

                case ActiveDamageBoostEffectId:
                    value *= 2f;
                    break;

                case TargetOutgoingDamageReductionEffectId:
                    value = Mathf.Max(0f, value - status.Stack);
                    break;

                case GritEffectId:
                    value *= 1.3f;
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
            damage = RuneEffectRules.ApplyWeaken(damage);

        if (GetStatusStack(statuses, ActiveDamageBoostEffectId) > 0)
            damage *= 2f;

        int fatigueStack = GetStatusStack(statuses, TargetOutgoingDamageReductionEffectId);
        if (fatigueStack > 0)
            damage = Mathf.Max(0f, damage - fatigueStack);

        if (GetStatusStack(statuses, GritEffectId) > 0)
            damage *= 1.3f;

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
        List<StatusEffectRuntimeData> statuses,
        CharacterRuntimeData playerAttackerRuntime)
    {
        if (GetStatusStack(statuses, VulnerableEffectId) > 0)
        {
            bool enhanced = BattleEquipmentEffectService.HasConfiguredEffect(
                playerAttackerRuntime,
                "E_Rune_Vulnerable_Bonus_Percent");
            damage = RuneEffectRules.ApplyVulnerable(damage, enhanced);
        }

        if (GetStatusStack(statuses, ActiveDamageReductionEffectId) > 0)
            damage *= 0.5f;

        int corrosionStack = GetStatusStack(statuses, CorrosionEffectId);
        if (corrosionStack > 0)
            damage += corrosionStack;

        return damage;
    }


    public static bool IsAttackingMonsterFromBehind(
        BattleCharacter attacker,
        MonsterUnit target)
    {
        if (attacker == null ||
            attacker.RuntimeData == null ||
            target == null ||
            target.RuntimeData == null ||
            attacker.CurrentGridIndex < 0 ||
            target.MainGridIndex < 0)
        {
            return false;
        }

        GridManager gridManager = Object.FindFirstObjectByType<GridManager>();
        if (gridManager == null)
            return false;

        Vector2Int attackerCoord = gridManager.IndexToCoord(attacker.CurrentGridIndex);
        Vector2Int targetCoord = gridManager.IndexToCoord(target.MainGridIndex);
        if (attackerCoord.x == targetCoord.x)
            return false;

        return target.RuntimeData.Direction == BattleDirection.Right
            ? attackerCoord.x < targetCoord.x
            : attackerCoord.x > targetCoord.x;
    }

    public static bool IsAttackingPlayerFromBehind(
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

    public static bool ResolveRearAttackQualification(
        bool? actionQualification,
        bool liveQualification)
    {
        return actionQualification ?? liveQualification;
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

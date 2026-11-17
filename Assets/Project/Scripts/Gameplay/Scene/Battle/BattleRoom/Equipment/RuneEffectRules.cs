using UnityEngine;
using Relic.Gameplay.Data;

public static class RuneEffectRules
{
    public static float ApplyVulnerable(float damage, bool enhanced) =>
        Mathf.Max(0f, damage) * (enhanced ? 1.75f : 1.5f);

    public static float ApplyWeaken(float damage) =>
        Mathf.Max(0f, damage) * 0.7f;

    public static bool IsAtOrBelowHalfHp(int currentHp, int maxHp) =>
        maxHp > 0 && currentHp * 2 <= maxHp;

    public static float ApplyLowHpDamagePercent(float damage, int percent, bool conditionMet) =>
        conditionMet && percent > 0
            ? Mathf.Max(0f, damage) * (1f + percent / 100f)
            : Mathf.Max(0f, damage);

    public static int ResolveGeneralRuneValueDelta(
        string effectId,
        SkillType skillType,
        int configuredValue)
    {
        if (configuredValue == 0)
            return 0;

        if (skillType == SkillType.Attack && BattleEquipmentEffectService.IsPlayerDamageEffect(effectId))
            return configuredValue;

        return effectId == "E_Cure" || effectId == "E_Armor" ||
               effectId == "E_Poison" || effectId == "E_Bleed"
            ? configuredValue
            : 0;
    }

    public static int ResolveConditionalDamageDelta(int hitCount, int livingEnemyCount) =>
        (hitCount >= 3 ? 1 : 0) + (livingEnemyCount == 1 ? 1 : 0);

    public static int ResolveFirstRegisteredAttackCountDelta(
        int earlierAttackReservationCount,
        bool isAttack) =>
        earlierAttackReservationCount == 0 && isAttack ? 1 : 0;

    public static bool IsFirstNonMoveMemory(int earlierSkillReservationCount, bool isMove) =>
        earlierSkillReservationCount == 0 && !isMove;

    public static bool MeetsArmorThresholdAtTurnEnd(int armor, int threshold) =>
        threshold > 0 && armor >= threshold;

    public static string ResolveTurnStartRuneStatus(string effectId) =>
        effectId == "E_Rune_Turn1_Ward" ? "E_Barrier" : string.Empty;
}

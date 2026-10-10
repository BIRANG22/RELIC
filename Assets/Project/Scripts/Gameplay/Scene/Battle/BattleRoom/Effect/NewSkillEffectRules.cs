using UnityEngine;

public static class NewSkillEffectRules
{
    public static int ResolveArmorStrikeDamage(int currentArmor, int valueRate) =>
        Mathf.Max(0, currentArmor) * Mathf.Max(0, valueRate);

    public static int ApplyBleedingDamageBonus(int damage, bool hasBleeding) =>
        hasBleeding ? Mathf.CeilToInt(Mathf.Max(0, damage) * 1.5f) : Mathf.Max(0, damage);

    public static int ResolveVulnerableStrikeDamage(int value, int vulnerable, int countRate) =>
        Mathf.Max(0, value) + Mathf.Max(0, vulnerable) * Mathf.Max(0, countRate);

    public static int ResolvePoisonByTargetPoison(int poison, int divisor) =>
        divisor > 0 ? Mathf.Max(0, poison) / divisor : 0;

    public static int ResolveTargetPoisonStrikeDamage(int poison) =>
        Mathf.Max(0, poison);

    public static int ResolveDirectDamageHitCount(string effectId, int configuredCount) =>
        effectId == "E_StrikeByVulnerable" || effectId == "E_StrikeByTargetPoison"
            ? 1
            : Mathf.Max(0, configuredCount);

    public static int ResolveDirectDamageParameterCount(string effectId, int configuredCount) =>
        effectId == "E_StrikeByVulnerable"
            ? Mathf.Max(0, configuredCount)
            : 1;

    public static bool ShouldApplyMoveCollisionEffect(string effectId) =>
        effectId != "E_Rush";

    public static int ResolveManaRestore(int successfulHitCount, int valueRate) =>
        Mathf.Max(0, successfulHitCount) * Mathf.Max(0, valueRate);

    public static int ResolvePermanentValueBonus(int killCount, int valueRate) =>
        Mathf.Max(0, killCount) * Mathf.Max(0, valueRate);

    public static int ResolveConditionalHitCount(int baseCount, int distinctEffectTypes) =>
        Mathf.Max(0, baseCount) + Mathf.Max(0, distinctEffectTypes);

    public static int ResolveValueUpOnHit(int successfulHitCount, int valueRate) =>
        Mathf.Max(0, successfulHitCount) * Mathf.Max(0, valueRate);
}

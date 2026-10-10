using UnityEngine;

public static class RuneEffectRules
{
    public static float ApplyVulnerable(float damage, bool enhanced) =>
        Mathf.Max(0f, damage) * (enhanced ? 1.75f : 1.5f);

    public static float ApplyWeaken(float damage) =>
        Mathf.Max(0f, damage) * 0.7f;

    public static bool IsAtOrBelowHalfHp(int currentHp, int maxHp) =>
        maxHp > 0 && currentHp * 2 <= maxHp;

    public static int ResolveConditionalDamageDelta(int hitCount, int targetCount) =>
        (hitCount >= 3 ? 1 : 0) + (targetCount == 1 ? 1 : 0);

    public static int ResolveFirstRegisteredAttackCountDelta(
        int timelineSlotIndex,
        bool isFirstSkillInSlot,
        bool isAttack) =>
        timelineSlotIndex == 0 && isFirstSkillInSlot && isAttack ? 1 : 0;
}

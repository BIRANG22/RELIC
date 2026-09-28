using System;
using Relic.Gameplay.Data;
using UnityEngine;

public static class SkillScalingUtility
{
    public const string None = "None";
    public const string MissingHP = "MissingHP";

    public static int ResolveBaseValue(CharacterRuntimeData runtime, SkillEffectEntry entry)
    {
        if (entry == null)
            return 0;

        bool usesMissingHp =
            string.Equals(entry.EffectId, "E_MissingHPStrike", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(entry.ScalingType, MissingHP, StringComparison.OrdinalIgnoreCase);

        if (usesMissingHp)
        {
            if (runtime == null)
                return Mathf.Max(0, entry.ValueAmount);

            int missingHp = Mathf.Max(0, runtime.MaxHP - runtime.CurrentHP);
            return Mathf.Max(0, Mathf.FloorToInt(missingHp * entry.ValueAmount * 0.01f));
        }

        return Mathf.Max(0, entry.ValueAmount);
    }
}

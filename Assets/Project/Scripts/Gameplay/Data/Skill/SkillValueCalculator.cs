using Relic.Gameplay.Data;

public static class SkillValueCalculator
{
    public static int GetValue(SkillEffectEntry entry)
    {
        if (entry == null)
            return 0;

        return entry.ValueAmount;
    }

    public static int GetCount(SkillEffectEntry entry)
    {
        if (entry == null)
            return 0;

        return entry.CountAmount;
    }
}

using Relic.Gameplay.Data;

public static class SkillValueCalculator
{
    public static int GetValue(SkillEffectEntry entry, int payAmount)
    {
        if (entry == null)
            return 0;

        return Calculate(entry.ValueCalcType, entry.ValueAmount, payAmount);
    }

    public static int GetCount(SkillEffectEntry entry)
    {
        if (entry == null)
            return 0;

        return entry.CountAmount;
    }

    private static int Calculate(
        ValueCalcType type,
        int amount,
        int payAmount)
    {
        return type switch
        {
            ValueCalcType.None => 0,
            ValueCalcType.Fixed => amount,
            ValueCalcType.PerCost => amount * payAmount,
            _ => 0
        };
    }
}
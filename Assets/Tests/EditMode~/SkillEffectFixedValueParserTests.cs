using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;

public sealed class SkillEffectFixedValueParserTests
{
    [Test]
    public void ParseSkill_UsesValueRateAsFixedValueWithoutCalcTypeColumn()
    {
        SkillMasterData skill = new()
        {
            SkillId = "S_FIXED_VALUE_ONLY",
            EffectIds = "E_Strike;E_Armor",
            ValueRate = "7;3",
            CountRate = "1;2"
        };

        List<SkillEffectEntry> entries = SkillEffectParser.Parse(skill, null);

        Assert.That(entries, Has.Count.EqualTo(2));
        Assert.That(entries[0].EffectId, Is.EqualTo("E_Strike"));
        Assert.That(entries[0].ValueAmount, Is.EqualTo(7));
        Assert.That(entries[0].CountAmount, Is.EqualTo(1));
        Assert.That(SkillValueCalculator.GetValue(entries[0]), Is.EqualTo(7));
        Assert.That(entries[1].EffectId, Is.EqualTo("E_Armor"));
        Assert.That(entries[1].ValueAmount, Is.EqualTo(3));
        Assert.That(entries[1].CountAmount, Is.EqualTo(2));
        Assert.That(SkillValueCalculator.GetValue(entries[1]), Is.EqualTo(3));
    }
}

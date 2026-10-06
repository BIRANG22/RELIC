using NUnit.Framework;
using Relic.Gameplay.Data;

public class PlayerSkillXReservationTests
{
    [Test]
    public void Command_ResolvesXAndEffectValuesAtReservationTime()
    {
        CharacterRuntimeData caster = new() { CharacterId = "Char_01", CurrentCost = 11 };
        SkillMasterData skill = new()
        {
            SkillId = "Skill_X",
            ReferenceResource = ReferenceResource.Cost,
            ResourceCostFormula = "3X",
            EffectIds = "E_Strike",
            ValueRate = "2X",
            CountRate = "1X"
        };

        PlayerReservedCommand command = new(caster, skill);
        caster.CurrentCost = 2;

        Assert.That(command.ResolvedX, Is.EqualTo(3));
        Assert.That(command.ResolvedResourceCost, Is.EqualTo(9));
        Assert.That(command.ResolvedEffectValues, Is.EqualTo(new[] { 6 }));
        Assert.That(command.ResolvedEffectCounts, Is.EqualTo(new[] { 3 }));
        Assert.That(command.Cost, Is.EqualTo(9));
    }

    [Test]
    public void Preview_UsesTheSameResolvedXForCountRate()
    {
        CharacterRuntimeData caster = new() { CharacterId = "Char_04", CurrentCost = 11 };
        SkillMasterData skill = new()
        {
            SkillId = "S_Core_73",
            ReferenceResource = ReferenceResource.Cost,
            ResourceCostFormula = "3X",
            EffectIds = "E_RandomStrike",
            ValueRate = "9",
            CountRate = "X"
        };

        PlayerReservedCommand command = new(caster, skill);
        BattlePlayerSkillPreview preview = BattlePlayerSkillPreviewCalculator.CreatePreview(command);

        Assert.That(preview.PayAmount, Is.EqualTo(9));
        Assert.That(preview.EffectValues, Is.EqualTo(new[] { 9 }));
        Assert.That(preview.EffectCounts, Is.EqualTo(new[] { 3 }));
        Assert.That(BattlePlayerSkillPreviewCalculator.GetTimelineValueText(preview), Is.EqualTo("9x3"));
    }
}

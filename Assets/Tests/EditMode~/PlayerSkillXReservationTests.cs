using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public class PlayerSkillXReservationTests
{
    [Test]
    public void TimelineReservation_RejectsXCostWhenResourceIsBelowOneUnit()
    {
        CharacterRuntimeData caster = new() { CharacterId = "Char_01", CurrentCost = 2 };
        SkillMasterData skill = new()
        {
            SkillId = "Skill_X",
            ReferenceResource = ReferenceResource.Cost,
            ResourceCostFormula = "3X"
        };
        PlayerReservedCommand command = new(caster, skill);
        GameObject controllerObject = new("BattleTimelineController_Test");

        try
        {
            BattleTimelineController controller = controllerObject.AddComponent<BattleTimelineController>();
            MethodInfo getReserveBlockReason = typeof(BattleTimelineController).GetMethod(
                "GetReserveBlockReason",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(getReserveBlockReason, Is.Not.Null);
            string blockReason = (string)getReserveBlockReason.Invoke(controller, new object[] { command });

            Assert.That(blockReason, Is.Not.Empty);
            Assert.That(command.ResolvedX, Is.Zero);
            Assert.That(command.Cost, Is.Zero);
        }
        finally
        {
            Object.DestroyImmediate(controllerObject);
        }
    }

    [TestCase(3, 1, 3)]
    [TestCase(11, 3, 9)]
    public void Command_XCostAtOrAboveOneUnit_ResolvesMaximumPayableMultiple(
        int currentMana,
        int expectedX,
        int expectedCost)
    {
        CharacterRuntimeData caster = new() { CharacterId = "Char_01", CurrentCost = currentMana };
        SkillMasterData skill = new()
        {
            SkillId = "Skill_X",
            ReferenceResource = ReferenceResource.Cost,
            ResourceCostFormula = "3X"
        };

        PlayerReservedCommand command = new(caster, skill);

        Assert.That(command.ResolvedX, Is.EqualTo(expectedX));
        Assert.That(command.Cost, Is.EqualTo(expectedCost));
        Assert.That(SkillCostCalculator.TryGetPreviewPayAmount(caster, skill, out int payAmount), Is.True);
        Assert.That(payAmount, Is.EqualTo(expectedCost));
    }

    [Test]
    public void Command_FixedZeroCost_RemainsUsable()
    {
        CharacterRuntimeData caster = new() { CharacterId = "Char_01", CurrentCost = 0 };
        SkillMasterData skill = new()
        {
            SkillId = "Skill_Free",
            ReferenceResource = ReferenceResource.Cost,
            ResourceCostFormula = "0"
        };

        PlayerReservedCommand command = new(caster, skill);

        Assert.That(command.IsSkillCostResolved, Is.True);
        Assert.That(command.Cost, Is.Zero);
        Assert.That(SkillCostCalculator.TryGetPreviewPayAmount(caster, skill, out int payAmount), Is.True);
        Assert.That(payAmount, Is.Zero);
    }

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

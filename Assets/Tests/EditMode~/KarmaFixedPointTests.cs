using NUnit.Framework;
using Relic.Gameplay.Data;
using System.Collections.Generic;

public sealed class KarmaFixedPointTests
{
    [Test]
    public void MigrateLegacyValue_ConvertsExactlyOnce()
    {
        CharacterRuntimeData runtime = new() { CurrentResource = 3 };
        KarmaFixedPoint.EnsureMigrated(runtime, true);
        KarmaFixedPoint.EnsureMigrated(runtime, true);
        Assert.That(runtime.CurrentResource, Is.EqualTo(6));
        Assert.That(KarmaFixedPoint.GetDisplayValue(runtime, true), Is.EqualTo(3f));
    }

    [Test]
    public void HalfUnitGainAndWholeCost_PreserveFraction()
    {
        CharacterRuntimeData runtime = new() { UniqueResourceUnitVersion = 1 };
        KarmaFixedPoint.AddHalfUnits(runtime, 3, 5, true);
        KarmaFixedPoint.SpendDisplayUnits(runtime, 1, true);
        Assert.That(runtime.CurrentResource, Is.EqualTo(1));
        Assert.That(KarmaFixedPoint.GetDisplayValue(runtime, true), Is.EqualTo(0.5f));
    }

    [Test]
    public void TurnPolicy_DeduplicatesSlotsAndIgnoresInvalid()
    {
        TurnEndKarmaPolicy policy = new();
        policy.Record("A", 0, true);
        policy.Record("A", 0, true);
        policy.Record("A", 1, true);
        policy.Record("A", 2, false);
        Assert.That(policy.GetHalfUnitGain("A"), Is.EqualTo(2));
    }

    [Test]
    public void HalfUnitGain_ClampsAtDisplayedMaximum()
    {
        CharacterRuntimeData runtime = new() { CurrentResource = 9, UniqueResourceUnitVersion = 1 };
        int gained = KarmaFixedPoint.AddHalfUnits(runtime, 5, 5, true);
        Assert.That(gained, Is.EqualTo(1));
        Assert.That(runtime.CurrentResource, Is.EqualTo(10));
    }

    [TestCase(Category.Unique, true)]
    [TestCase(Category.Ability, true)]
    [TestCase(Category.Public, true)]
    [TestCase(Category.Core, true)]
    [TestCase(Category.Move, false)]
    [TestCase(Category.Passive, false)]
    public void MemoryCategory_MatchesActiveMemoryClassification(Category category, bool expected)
    {
        Assert.That(TurnEndKarmaPolicy.IsMemoryCategory(category), Is.EqualTo(expected));
    }

    [Test]
    public void TurnPolicy_ExposesGainByStableCharacterId()
    {
        TurnEndKarmaPolicy policy = new();
        policy.Record("Char_A", 0, true);
        policy.Record("Char_A", 1, true);
        policy.Record("Char_B", 4, true);

        Dictionary<string, int> gains = new(policy.GetRecordedGains());
        Assert.That(gains["Char_A"], Is.EqualTo(2));
        Assert.That(gains["Char_B"], Is.EqualTo(1));
    }

    [Test]
    public void TurnPolicy_FromExecutedCommands_UsesFinalExecutionResults()
    {
        CharacterRuntimeData runtime = new() { CharacterId = "Char_A" };
        PlayerReservedCommand slot0 = CreateExecutedMemory(runtime, "Skill_0", 0, true);
        PlayerReservedCommand slot1 = CreateExecutedMemory(runtime, "Skill_1", 1, true);
        PlayerReservedCommand invalid = CreateExecutedMemory(runtime, "Skill_Invalid", 2, false);

        TurnEndKarmaPolicy policy = TurnEndKarmaPolicy.FromExecutedCommands(
            new[] { slot0, slot1, slot0, invalid });

        Assert.That(policy.GetHalfUnitGain("Char_A"), Is.EqualTo(2));
    }

    [Test]
    public void ExecutedMemoryWithoutImpact_IsStillEligibleForTurnEndKarma()
    {
        CharacterRuntimeData runtime = new() { CharacterId = "Char_A" };
        SkillMasterData skill = new() { SkillId = "Skill_0", Category = Category.Core };
        PlayerReservedCommand command = new(runtime, skill);
        command.SetTimelineSlotIndex(0);

        command.MarkValidExecution();

        Assert.That(command.ExecutionResult.AttemptedHitCount, Is.Zero);
        Assert.That(command.ExecutionResult.WasValidExecution, Is.True);
        Assert.That(
            TurnEndKarmaPolicy.FromExecutedCommands(new[] { command })
                .GetHalfUnitGain("Char_A"),
            Is.EqualTo(1));
    }

    private static PlayerReservedCommand CreateExecutedMemory(
        CharacterRuntimeData runtime,
        string skillId,
        int slotIndex,
        bool valid)
    {
        SkillMasterData skill = new() { SkillId = skillId, Category = Category.Core };
        PlayerReservedCommand command = new(runtime, skill);
        command.SetTimelineSlotIndex(slotIndex);
        command.ExecutionResult.WasValidExecution = valid;
        return command;
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;

public sealed class CharacterLevelUnlockServiceTests
{
    [Test]
    public void UnlockLevels_FallbacksPreserveCurrentLobbyRules()
    {
        CharacterMasterData character = new()
        {
            CharacterId = "Char_A"
        };

        RuneData rune = new()
        {
            RuneId = "Rune_A",
            TargetCharacterId = "Char_A",
            UnlockLevel = 9
        };

        Assert.Multiple(() =>
        {
            Assert.That(CharacterLevelUnlockService.GetRuneSlotUnlockLevel(character, 0), Is.EqualTo(1));
            Assert.That(CharacterLevelUnlockService.GetRuneSlotUnlockLevel(character, 1), Is.EqualTo(1));
            Assert.That(CharacterLevelUnlockService.GetRuneSlotUnlockLevel(character, 2), Is.EqualTo(3));
            Assert.That(CharacterLevelUnlockService.GetRuneSlotUnlockLevel(character, 3), Is.EqualTo(5));
            Assert.That(CharacterLevelUnlockService.GetRuneSlotUnlockLevel(character, 4), Is.EqualTo(7));
            Assert.That(CharacterLevelUnlockService.GetRuneSlotUnlockLevel(character, 5), Is.EqualTo(10));
            Assert.That(CharacterLevelUnlockService.GetRuneUnlockLevel(character, rune, 0), Is.EqualTo(9));
            Assert.That(CharacterLevelUnlockService.GetSkillMemoryUnlockLevel(character, 0, 0), Is.EqualTo(1));
            Assert.That(CharacterLevelUnlockService.GetSkillMemoryUnlockLevel(character, 0, 1), Is.EqualTo(5));
            Assert.That(CharacterLevelUnlockService.GetSkillMemoryUnlockLevel(character, 1, 0), Is.EqualTo(1));
            Assert.That(CharacterLevelUnlockService.GetSkillMemoryUnlockLevel(character, 1, 1), Is.EqualTo(10));
            Assert.That(CharacterLevelUnlockService.GetSkillMemoryUnlockLevel(character, 2, 1), Is.EqualTo(1));
        });
    }

    [Test]
    public void UnlockLevels_UseCharacterSpecificOverrides()
    {
        CharacterMasterData character = new()
        {
            CharacterId = "Char_A",
            RuneSlotUnlockLevels = new[] { 1, 2, 4 },
            RuneUnlockLevels = new[] { 3, 8 },
            PassiveSkillUnlockLevels = new[] { 1, 6 },
            UniqueSkillUnlockLevels = new[] { 1, 11 },
            CharacterSkillUnlockLevels = new[] { 1, 12 }
        };

        RuneData rune = new()
        {
            RuneId = "Rune_A",
            TargetCharacterId = "Char_A",
            UnlockLevel = 9
        };

        Assert.Multiple(() =>
        {
            Assert.That(CharacterLevelUnlockService.GetRuneSlotUnlockLevel(character, 1), Is.EqualTo(2));
            Assert.That(CharacterLevelUnlockService.GetRuneSlotUnlockLevel(character, 2), Is.EqualTo(4));
            Assert.That(CharacterLevelUnlockService.GetRuneUnlockLevel(character, rune, 1), Is.EqualTo(8));
            Assert.That(CharacterLevelUnlockService.GetSkillMemoryUnlockLevel(character, 0, 1), Is.EqualTo(6));
            Assert.That(CharacterLevelUnlockService.GetSkillMemoryUnlockLevel(character, 1, 1), Is.EqualTo(11));
            Assert.That(CharacterLevelUnlockService.GetSkillMemoryUnlockLevel(character, 2, 1), Is.EqualTo(12));
        });
    }

    [Test]
    public void UnlockTexts_ReturnsRewardsCrossedByLevelRange()
    {
        CharacterMasterData character = new()
        {
            CharacterId = "Char_A",
            Rune1 = "Rune_A",
            Rune2 = "Rune_B",
            PassiveSkill1 = "S_Passive_A1",
            PassiveSkill2 = "S_Passive_A2",
            UniqueSkill1 = "S_Unique_A1",
            UniqueSkill2 = "S_Unique_A2",
            CharacterSkill1 = "S_Ability_A1",
            CharacterSkill2 = "S_Ability_A2",
            RuneSlotUnlockLevels = new[] { 1, 1, 4 },
            RuneUnlockLevels = new[] { 2, 5 },
            PassiveSkillUnlockLevels = new[] { 1, 6 },
            UniqueSkillUnlockLevels = new[] { 1, 9 },
            CharacterSkillUnlockLevels = new[] { 1, 1 }
        };

        RuneDatabase runeDatabase = new();
        runeDatabase.Initialize(new[]
        {
            new RuneData { RuneId = "Rune_A", TargetCharacterId = "Char_A", UnlockLevel = 2 },
            new RuneData { RuneId = "Rune_B", TargetCharacterId = "Char_A", UnlockLevel = 5 }
        });

        SkillDatabase skillDatabase = new();
        skillDatabase.Initialize(new[]
        {
            new SkillMasterData { SkillId = "S_Passive_A1", Name = "첫 기억" },
            new SkillMasterData { SkillId = "S_Passive_A2", Name = "새 기억" },
            new SkillMasterData { SkillId = "S_Unique_A1", Name = "첫 발현" },
            new SkillMasterData { SkillId = "S_Unique_A2", Name = "새 발현" },
            new SkillMasterData { SkillId = "S_Ability_A1", Name = "기본 능력" },
            new SkillMasterData { SkillId = "S_Ability_A2", Name = "추가 능력" }
        });

        IReadOnlyList<string> unlocks = CharacterLevelUnlockService.GetUnlockTexts(
            character,
            runeDatabase,
            skillDatabase,
            3,
            6);

        Assert.Multiple(() =>
        {
            Assert.That(unlocks, Has.Count.EqualTo(3));
            Assert.That(unlocks, Does.Contain("룬 슬롯 해금"));
            Assert.That(unlocks, Does.Contain("룬 해금"));
            Assert.That(unlocks, Does.Contain("기억 해금"));
        });
    }

    [Test]
    public void UnlockTexts_ReturnsEmptyWhenLevelDidNotIncrease()
    {
        CharacterMasterData character = new()
        {
            CharacterId = "Char_A",
            RuneSlotUnlockLevels = new[] { 1, 1, 4 },
            PassiveSkillUnlockLevels = new[] { 1, 6 }
        };

        IReadOnlyList<string> unlocks = CharacterLevelUnlockService.GetUnlockTexts(
            character,
            null,
            null,
            5,
            5);

        Assert.That(unlocks, Is.Empty);
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;

public sealed class DemoBattlePartySetupTests
{
    [Test]
    public void TryPrepare_CreatesRequestedPartyWithEmptyRunesAndRelics()
    {
        CharacterDatabase characterDatabase = CreateCharacterDatabase();
        CharacterRuntimeStore characterStore = new();
        PartyRuntimeStore partyStore = new();
        LobbyRuntimeData lobby = CreateDirtyLobbyRuntime();
        BattleRuntimeData battle = CreateDirtyBattleRuntime();

        bool prepared = DemoBattlePartySetup.TryPrepare(
            characterDatabase,
            characterStore,
            partyStore,
            lobby,
            battle);

        Assert.That(prepared, Is.True);
        Assert.That(partyStore.GetCharacterId(0), Is.EqualTo("Char_01"));
        Assert.That(partyStore.GetCharacterId(1), Is.EqualTo("Char_02"));
        Assert.That(partyStore.GetCharacterId(2), Is.EqualTo("Char_04"));
        Assert.That(partyStore.GetSpawnGridIndex(0), Is.EqualTo(0));
        Assert.That(partyStore.GetSpawnGridIndex(1), Is.EqualTo(1));
        Assert.That(partyStore.GetSpawnGridIndex(2), Is.EqualTo(2));

        AssertEmptyEquipment(characterStore.Get("Char_01"));
        AssertEmptyEquipment(characterStore.Get("Char_02"));
        AssertEmptyEquipment(characterStore.Get("Char_04"));

        Assert.That(lobby.OwnedRelicIds, Is.Empty);
        Assert.That(lobby.SkillInventoryIds, Is.Empty);
        Assert.That(lobby.BagItemIds, Is.Empty);
        Assert.That(lobby.CharacterLoadouts, Has.Count.EqualTo(3));
        Assert.That(battle.Remnant, Is.Zero);
        Assert.That(battle.OwnedRelicIds, Is.Empty);
        Assert.That(battle.SkillInventoryIds, Is.Empty);
        Assert.That(battle.BagItemIds, Is.Empty);
        Assert.That(battle.IsBattleRunInitialized, Is.True);
        Assert.That(battle.IsDemoBattle, Is.True);
    }

    [Test]
    public void TryPrepare_WhenRequiredCharacterIsMissing_DoesNotLeavePartialParty()
    {
        CharacterDatabase characterDatabase = new();
        characterDatabase.Initialize(new[]
        {
            CreateMaster("Char_01"),
            CreateMaster("Char_02")
        });
        CharacterRuntimeStore characterStore = new();
        PartyRuntimeStore partyStore = new();

        bool prepared = DemoBattlePartySetup.TryPrepare(
            characterDatabase,
            characterStore,
            partyStore,
            new LobbyRuntimeData(),
            new BattleRuntimeData());

        Assert.That(prepared, Is.False);
        Assert.That(partyStore.HasAnyCharacter, Is.False);
        Assert.That(characterStore.GetAll(), Is.Empty);
    }

    private static CharacterDatabase CreateCharacterDatabase()
    {
        CharacterDatabase database = new();
        database.Initialize(new[]
        {
            CreateMaster("Char_01"),
            CreateMaster("Char_02"),
            CreateMaster("Char_04")
        });
        return database;
    }

    private static CharacterMasterData CreateMaster(string characterId)
    {
        return new CharacterMasterData
        {
            CharacterId = characterId,
            MaxHP = 20,
            MaxCost = 10,
            CostRecovery = 3,
            PassiveSkill1 = $"Passive_{characterId}",
            UniqueSkill1 = $"Unique_{characterId}",
            CharacterSkill1 = $"Ability_{characterId}"
        };
    }

    private static LobbyRuntimeData CreateDirtyLobbyRuntime()
    {
        return new LobbyRuntimeData
        {
            OwnedRelicIds = new List<string> { "Relic_Old" },
            SkillInventoryIds = new List<string> { "Skill_Old" },
            BagItemIds = new List<string> { "Item_Old" }
        };
    }

    private static BattleRuntimeData CreateDirtyBattleRuntime()
    {
        return new BattleRuntimeData
        {
            Remnant = 99,
            OwnedRelicIds = new List<string> { "Relic_Old" },
            SkillInventoryIds = new List<string> { "Skill_Old" },
            BagItemIds = new List<string> { "Item_Old" },
            IsBattleRunInitialized = true
        };
    }

    private static void AssertEmptyEquipment(CharacterRuntimeData character)
    {
        Assert.That(character, Is.Not.Null);
        Assert.That(character.EquippedRuneIds, Has.Length.EqualTo(6));
        Assert.That(character.EquippedRuneIds, Has.All.Empty);
        Assert.That(character.EquippedRelicIds, Has.Length.EqualTo(7));
        Assert.That(character.EquippedRelicIds, Has.All.Empty);
    }
}

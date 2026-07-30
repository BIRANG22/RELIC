using NUnit.Framework;
using Relic.Gameplay.Data;

public class InitialDefaultPartySetupTests
{
    [Test]
    public void TryInitialize_EmptyPartyCreatesThreeDefaultCharactersAndRuntimes()
    {
        CharacterDatabase database = CreateDatabase(
            CreateMaster("Char_01", 101),
            CreateMaster("Char_02", 102),
            CreateMaster("Char_03", 103));
        CharacterRuntimeStore characterStore = new();
        PartyRuntimeStore partyStore = new();

        bool result = InitialDefaultPartySetup.TryInitialize(
            database,
            characterStore,
            partyStore,
            null);

        Assert.That(result, Is.True);
        Assert.That(partyStore.GetCharacterId(0), Is.EqualTo("Char_01"));
        Assert.That(partyStore.GetCharacterId(1), Is.EqualTo("Char_02"));
        Assert.That(partyStore.GetCharacterId(2), Is.EqualTo("Char_03"));
        Assert.That(partyStore.GetSpawnGridIndex(0), Is.EqualTo(6));
        Assert.That(partyStore.GetSpawnGridIndex(1), Is.EqualTo(7));
        Assert.That(partyStore.GetSpawnGridIndex(2), Is.EqualTo(8));
        Assert.That(characterStore.GetAll().Count, Is.EqualTo(3));
        Assert.That(characterStore.Get("Char_01").MaxHP, Is.EqualTo(101));
        Assert.That(characterStore.Get("Char_02").CurrentHP, Is.EqualTo(102));
        Assert.That(characterStore.Get("Char_03").EquippedSkillIds, Is.EqualTo(
            new[] { "Unique_Char_03", "Ability_Char_03", "Common_Char_03", string.Empty }));
    }

    [Test]
    public void TryInitialize_ExistingPartyPreservesPartyAndCharacterRuntime()
    {
        CharacterDatabase database = CreateDatabase(
            CreateMaster("Char_01", 101),
            CreateMaster("Char_02", 102),
            CreateMaster("Char_03", 103));
        CharacterRuntimeStore characterStore = new();
        CharacterRuntimeData existingRuntime = new()
        {
            CharacterId = "Existing",
            Level = 9
        };
        characterStore.AddOrUpdate(existingRuntime);
        PartyRuntimeStore partyStore = new();
        partyStore.SetSlot(1, "Existing", 12);

        bool result = InitialDefaultPartySetup.TryInitialize(
            database,
            characterStore,
            partyStore,
            null);

        Assert.That(result, Is.True);
        Assert.That(partyStore.GetCharacterId(0), Is.Null);
        Assert.That(partyStore.GetCharacterId(1), Is.EqualTo("Existing"));
        Assert.That(partyStore.GetCharacterId(2), Is.Null);
        Assert.That(partyStore.GetSpawnGridIndex(1), Is.EqualTo(12));
        Assert.That(characterStore.GetAll().Count, Is.EqualTo(1));
        Assert.That(characterStore.Get("Existing"), Is.SameAs(existingRuntime));
        Assert.That(characterStore.Get("Existing").Level, Is.EqualTo(9));
    }

    [Test]
    public void TryInitialize_MissingRequiredMasterLeavesStoresUntouched()
    {
        CharacterDatabase database = CreateDatabase(
            CreateMaster("Char_01", 101),
            CreateMaster("Char_02", 102));
        CharacterRuntimeStore characterStore = new();
        CharacterRuntimeData retainedRuntime = new()
        {
            CharacterId = "Retained",
            Level = 7
        };
        characterStore.AddOrUpdate(retainedRuntime);
        PartyRuntimeStore partyStore = new();

        bool result = InitialDefaultPartySetup.TryInitialize(
            database,
            characterStore,
            partyStore,
            null);

        Assert.That(result, Is.False);
        Assert.That(partyStore.HasAnyCharacter, Is.False);
        Assert.That(characterStore.GetAll().Count, Is.EqualTo(1));
        Assert.That(characterStore.Get("Retained"), Is.SameAs(retainedRuntime));
    }

    private static CharacterDatabase CreateDatabase(params CharacterMasterData[] masters)
    {
        CharacterDatabase database = new();
        database.Initialize(masters);
        return database;
    }

    private static CharacterMasterData CreateMaster(string characterId, int maxHp)
    {
        return new CharacterMasterData
        {
            CharacterId = characterId,
            MaxHP = maxHp,
            MaxCost = 10,
            CostRecovery = 4,
            IsDefaultProvided = true,
            PassiveSkill1 = "Passive_" + characterId,
            UniqueSkill1 = "Unique_" + characterId,
            CharacterSkill1 = "Ability_" + characterId,
            CommonSkill1 = "Common_" + characterId
        };
    }
}

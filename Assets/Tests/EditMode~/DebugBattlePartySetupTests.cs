using System.Linq;
using NUnit.Framework;
using Relic.Gameplay.Data;

public class DebugBattlePartySetupTests
{
    [Test]
    public void TryCreateDefaultParty_UsesUpToThreeDefaultCharactersInIdOrder()
    {
        CharacterDatabase database = new();
        database.Initialize(new[]
        {
            CreateMaster("Char_03", true, 130),
            CreateMaster("Char_01", true, 110),
            CreateMaster("Char_04", false, 140),
            CreateMaster("Char_02", true, 120),
            CreateMaster("Char_00", true, 100)
        });

        CharacterRuntimeStore characterStore = new();
        PartyRuntimeStore partyStore = new();

        bool result = DebugBattlePartySetup.TryCreateDefaultParty(
            database,
            characterStore,
            partyStore,
            null);

        Assert.That(result, Is.True);
        Assert.That(partyStore.GetCharacterId(0), Is.EqualTo("Char_00"));
        Assert.That(partyStore.GetCharacterId(1), Is.EqualTo("Char_01"));
        Assert.That(partyStore.GetCharacterId(2), Is.EqualTo("Char_02"));
        Assert.That(partyStore.GetSpawnGridIndex(0), Is.EqualTo(0));
        Assert.That(partyStore.GetSpawnGridIndex(1), Is.EqualTo(1));
        Assert.That(partyStore.GetSpawnGridIndex(2), Is.EqualTo(2));
        Assert.That(characterStore.GetAll().Count, Is.EqualTo(3));
        Assert.That(characterStore.Get("Char_01").CurrentHP, Is.EqualTo(110));
        Assert.That(characterStore.Get("Char_01").MaxHP, Is.EqualTo(110));
        Assert.That(characterStore.Get("Char_01").PassiveSkillId, Is.EqualTo("P_Char_01"));
    }

    [Test]
    public void TryCreateDefaultParty_ReturnsFalseAndClearsStoresWhenNoDefaultCharacterExists()
    {
        CharacterDatabase database = new();
        database.Initialize(new[] { CreateMaster("Char_01", false, 100) });

        CharacterRuntimeStore characterStore = new();
        characterStore.AddOrUpdate(new CharacterRuntimeData { CharacterId = "Old" });
        PartyRuntimeStore partyStore = new();
        partyStore.SetSlot(0, "Old", 0);

        bool result = DebugBattlePartySetup.TryCreateDefaultParty(
            database,
            characterStore,
            partyStore,
            null);

        Assert.That(result, Is.False);
        Assert.That(characterStore.GetAll(), Is.Empty);
        Assert.That(partyStore.HasAnyCharacter, Is.False);
    }

    [Test]
    public void TryCreateDefaultParty_EquipsSkillVfxTestSkillForChar03()
    {
        CharacterDatabase database = new();
        database.Initialize(new[]
        {
            CreateMaster("Char_01", true, 100),
            CreateMaster("Char_02", true, 100),
            CreateMaster("Char_03", true, 100)
        });

        CharacterRuntimeStore characterStore = new();
        PartyRuntimeStore partyStore = new();

        bool result = DebugBattlePartySetup.TryCreateDefaultParty(
            database,
            characterStore,
            partyStore,
            null);

        Assert.That(result, Is.True);
        CharacterRuntimeData runtime = characterStore.Get("Char_03");
        Assert.That(runtime, Is.Not.Null);
        Assert.That(runtime.EquippedSkillIds.Contains("S_Ability_11"), Is.True);
        Assert.That(runtime.EquippedSkillIds[3], Is.EqualTo("S_Ability_11"));
    }

    private static CharacterMasterData CreateMaster(string id, bool isDefaultProvided, int maxHp)
    {
        return new CharacterMasterData
        {
            CharacterId = id,
            MaxHP = maxHp,
            MaxCost = 10,
            CostRecovery = 3,
            IsDefaultProvided = isDefaultProvided,
            PassiveSkill1 = $"P_{id}",
            UniqueSkill1 = $"U_{id}",
            CharacterSkill1 = $"A_{id}",
            CommonSkill1 = $"C_{id}"
        };
    }
}

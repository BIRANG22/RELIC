using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;

public class LobbySkillUpgradeServiceTests
{
    [TestCase(0, 100)]
    [TestCase(1, 150)]
    [TestCase(2, 200)]
    public void PricePolicy_IncreasesByFiftyPerSuccessfulUpgrade(int count, int expected)
    {
        Assert.That(LobbySkillUpgradePricePolicy.GetPrice(count), Is.EqualTo(expected));
    }

    [Test]
    public void UpgradeCharacterAbility_DeductsPriceAndIncrementsCount()
    {
        var lobby = new LobbyRuntimeData { BlueDustium = 300, LobbySkillUpgradeCount = 1 };
        var characters = new CharacterRuntimeStore();
        characters.AddOrUpdate(new CharacterRuntimeData
        {
            CharacterId = "Character_A",
            AbilitySkillId = "S_Ability_01"
        });
        var service = new LobbySkillUpgradeService(characters);

        LobbySkillUpgradeResult result = service.Execute(
            lobby,
            new LobbySkillUpgradeCommand(
                "Character_A",
                "S_Ability_01",
                "S_Ability_02",
                SkillSlotType.Ability,
                -1));

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.Price, Is.EqualTo(150));
        Assert.That(lobby.BlueDustium, Is.EqualTo(150));
        Assert.That(lobby.LobbySkillUpgradeCount, Is.EqualTo(2));
        Assert.That(characters.Get("Character_A").AbilitySkillId, Is.EqualTo("S_Ability_02"));
    }

    [Test]
    public void UpgradeInventorySkill_ChangesLobbyInventoryOnly()
    {
        var lobby = new LobbyRuntimeData
        {
            BlueDustium = 999,
            SkillInventoryIds = new List<string> { "S_Public_01" }
        };
        var service = new LobbySkillUpgradeService(new CharacterRuntimeStore());

        LobbySkillUpgradeResult result = service.Execute(
            lobby,
            new LobbySkillUpgradeCommand(
                null,
                "S_Public_01",
                "S_Public_02",
                SkillSlotType.Inventory,
                0));

        Assert.That(result.Succeeded, Is.True);
        Assert.That(lobby.SkillInventoryIds[0], Is.EqualTo("S_Public_02"));
        Assert.That(lobby.BlueDustium, Is.EqualTo(899));
        Assert.That(lobby.LobbySkillUpgradeCount, Is.EqualTo(1));
    }

    [Test]
    public void InsufficientBlueDustium_DoesNotMutateSkillOrCount()
    {
        var lobby = new LobbyRuntimeData { BlueDustium = 99 };
        var characters = new CharacterRuntimeStore();
        characters.AddOrUpdate(new CharacterRuntimeData
        {
            CharacterId = "Character_A",
            AbilitySkillId = "S_Ability_01"
        });
        var service = new LobbySkillUpgradeService(characters);

        LobbySkillUpgradeResult result = service.Execute(
            lobby,
            new LobbySkillUpgradeCommand(
                "Character_A",
                "S_Ability_01",
                "S_Ability_02",
                SkillSlotType.Ability,
                -1));

        Assert.That(result.Failure, Is.EqualTo(LobbySkillUpgradeFailure.InsufficientBlueDustium));
        Assert.That(lobby.BlueDustium, Is.EqualTo(99));
        Assert.That(lobby.LobbySkillUpgradeCount, Is.EqualTo(0));
        Assert.That(characters.Get("Character_A").AbilitySkillId, Is.EqualTo("S_Ability_01"));
    }

    [Test]
    public void SelectedRequest_ExecutesUpgradeAndConsumesBlueDustium()
    {
        var lobby = new LobbyRuntimeData { BlueDustium = 999 };
        var characters = new CharacterRuntimeStore();
        characters.AddOrUpdate(new CharacterRuntimeData
        {
            CharacterId = "Character_A",
            EquippedSkillIds = new[] { null, null, "S_Public_01" }
        });
        var selection = new LobbySkillUpgradeSelection();
        selection.Select(new SkillUpgradeRequest
        {
            CharacterId = "Character_A",
            CurrentSkillId = "S_Public_01",
            UpgradeSkillId = "S_Public_02",
            SlotType = SkillSlotType.Equipped,
            SlotIndex = 2
        });

        LobbySkillUpgradeResult result = selection.Execute(
            lobby,
            new LobbySkillUpgradeService(characters));

        Assert.That(result.Succeeded, Is.True);
        Assert.That(lobby.BlueDustium, Is.EqualTo(899));
        Assert.That(lobby.LobbySkillUpgradeCount, Is.EqualTo(1));
        Assert.That(characters.Get("Character_A").EquippedSkillIds[2], Is.EqualTo("S_Public_02"));
        Assert.That(selection.HasSelection, Is.False);
    }

    [Test]
    public void UpgradeAbility_AlsoUpdatesMirroredEquippedSlot()
    {
        var lobby = new LobbyRuntimeData { BlueDustium = 999 };
        var characters = new CharacterRuntimeStore();
        characters.AddOrUpdate(new CharacterRuntimeData
        {
            CharacterId = "Character_A",
            AbilitySkillId = "S_Ability_01",
            EquippedSkillIds = new[] { "S_Unique_01", "S_Ability_01", "S_Public_01", "" }
        });

        LobbySkillUpgradeResult result = new LobbySkillUpgradeService(characters).Execute(
            lobby,
            new LobbySkillUpgradeCommand(
                "Character_A",
                "S_Ability_01",
                "S_Ability_02",
                SkillSlotType.Ability,
                -1));

        Assert.That(result.Succeeded, Is.True);
        Assert.That(characters.Get("Character_A").AbilitySkillId, Is.EqualTo("S_Ability_02"));
        Assert.That(characters.Get("Character_A").EquippedSkillIds[1], Is.EqualTo("S_Ability_02"));
    }

    [Test]
    public void RecordedUpgrade_RestoresCharacterAfterAnotherPanelOverwritesIt()
    {
        var lobby = new LobbyRuntimeData();
        var characters = new CharacterRuntimeStore();
        var character = new CharacterRuntimeData
        {
            CharacterId = "Character_A",
            AbilitySkillId = "S_Ability_01",
            EquippedSkillIds = new[] { "S_Unique_01", "S_Ability_01", "S_Public_01", "" }
        };
        characters.AddOrUpdate(character);
        LobbySkillUpgradePersistence.Record(
            lobby,
            new LobbySkillUpgradeCommand(
                "Character_A", "S_Ability_01", "S_Ability_02", SkillSlotType.Ability, -1));

        bool changed = LobbySkillUpgradePersistence.ApplyAll(lobby, characters);

        Assert.That(changed, Is.True);
        Assert.That(character.AbilitySkillId, Is.EqualTo("S_Ability_02"));
        Assert.That(character.EquippedSkillIds[1], Is.EqualTo("S_Ability_02"));
    }
}

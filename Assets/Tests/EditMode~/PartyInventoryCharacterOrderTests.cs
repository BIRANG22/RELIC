using NUnit.Framework;
using Relic.Gameplay.Data;

public class PartyInventoryCharacterOrderTests
{
    [Test]
    public void Build_SkipsEmptyPartySlotsAndPreservesPartyOrder()
    {
        var party = new PartyRuntimeStore();
        party.SetCharacter(0, "Character_A");
        party.SetCharacter(2, "Character_C");

        PartyInventoryCharacterEntry[] result = PartyInventoryCharacterOrder.Build(party, 3);

        Assert.That(result[0].CharacterId, Is.EqualTo("Character_A"));
        Assert.That(result[0].PartySlotIndex, Is.EqualTo(0));
        Assert.That(result[1].CharacterId, Is.EqualTo("Character_C"));
        Assert.That(result[1].PartySlotIndex, Is.EqualTo(2));
        Assert.That(result[2].CharacterId, Is.Null);
        Assert.That(result[2].PartySlotIndex, Is.EqualTo(-1));
    }

    [Test]
    public void Build_ReflectsCharactersRemovedAndReorderedAfterPreviousRead()
    {
        var party = new PartyRuntimeStore();
        party.SetCharacter(0, "Character_A");
        party.SetCharacter(1, "Character_B");
        PartyInventoryCharacterOrder.Build(party, 3);

        party.ClearSlot(0);
        party.ClearSlot(1);
        party.SetCharacter(0, "Character_C");

        PartyInventoryCharacterEntry[] result = PartyInventoryCharacterOrder.Build(party, 3);

        Assert.That(result[0].CharacterId, Is.EqualTo("Character_C"));
        Assert.That(result[1].CharacterId, Is.Null);
        Assert.That(result[2].CharacterId, Is.Null);
    }
}

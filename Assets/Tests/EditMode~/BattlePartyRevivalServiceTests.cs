using NUnit.Framework;
using Relic.Gameplay.Data;

public class BattlePartyRevivalServiceTests
{
    [Test]
    public void ReviveIncapacitatedParty_RevivesOnlyDeadCurrentPartyMembersAtOneHp()
    {
        PartyRuntimeStore party = new PartyRuntimeStore();
        party.SetCharacter(0, "Dead");
        party.SetCharacter(1, "Alive");

        CharacterRuntimeData dead = new CharacterRuntimeData
        {
            CharacterId = "Dead",
            MaxHP = 20,
            CurrentHP = 0
        };
        CharacterRuntimeData alive = new CharacterRuntimeData
        {
            CharacterId = "Alive",
            MaxHP = 20,
            CurrentHP = 7
        };
        CharacterRuntimeData nonPartyDead = new CharacterRuntimeData
        {
            CharacterId = "NonPartyDead",
            MaxHP = 20,
            CurrentHP = 0
        };

        CharacterRuntimeStore characters = new CharacterRuntimeStore();
        characters.AddOrUpdate(dead);
        characters.AddOrUpdate(alive);
        characters.AddOrUpdate(nonPartyDead);

        int revivedCount = BattlePartyRevivalService.ReviveIncapacitatedParty(party, characters);

        Assert.That(revivedCount, Is.EqualTo(1));
        Assert.That(dead.CurrentHP, Is.EqualTo(1));
        Assert.That(alive.CurrentHP, Is.EqualTo(7));
        Assert.That(nonPartyDead.CurrentHP, Is.Zero);
    }

    [Test]
    public void ReviveIncapacitatedParty_NullStores_ReturnsZero()
    {
        Assert.That(
            BattlePartyRevivalService.ReviveIncapacitatedParty(null, null),
            Is.Zero);
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;

public class LobbyBattleRuntimeTransferServiceTests
{
    [Test]
    public void Transfer_CopiesCombatInventoryWithoutSharingLists()
    {
        var lobby = new LobbyRuntimeData
        {
            BlueDustium = 432,
            OwnedRelicIds = new List<string> { "R_A" },
            SkillInventoryIds = new List<string> { "S_A" },
            BagItemIds = new List<string> { "I_A" }
        };
        var battle = new BattleRuntimeData();

        LobbyBattleRuntimeTransferResult result =
            new LobbyBattleRuntimeTransferService().Transfer(lobby, battle, new CharacterRuntimeStore());

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(battle.OwnedRelicIds, Is.EquivalentTo(lobby.OwnedRelicIds));
            Assert.That(battle.SkillInventoryIds, Is.EquivalentTo(lobby.SkillInventoryIds));
            Assert.That(battle.BagItemIds, Is.EquivalentTo(lobby.BagItemIds));
            Assert.That(battle.OwnedRelicIds, Is.Not.SameAs(lobby.OwnedRelicIds));
            Assert.That(lobby.BlueDustium, Is.EqualTo(432));
        });
    }

    [Test]
    public void Transfer_AppliesLoadoutByCharacterId()
    {
        var character = new CharacterRuntimeData { CharacterId = "Character_A" };
        var characters = new CharacterRuntimeStore();
        characters.AddOrUpdate(character);
        var lobby = new LobbyRuntimeData
        {
            CharacterLoadouts = new List<LobbyCharacterLoadoutData>
            {
                new()
                {
                    CharacterId = "Character_A",
                    EquippedRelicIds = new[] { "R_A", null, null, null, null },
                    EquippedSkillIds = new[] { "S_A", null, null, null }
                }
            }
        };

        LobbyBattleRuntimeTransferResult result =
            new LobbyBattleRuntimeTransferService().Transfer(lobby, new BattleRuntimeData(), characters);

        Assert.That(result.Succeeded, Is.True);
        Assert.That(character.EquippedRelicIds[0], Is.EqualTo("R_A"));
        Assert.That(character.EquippedSkillIds[0], Is.EqualTo("S_A"));
        Assert.That(character.EquippedRelicIds, Is.Not.SameAs(lobby.CharacterLoadouts[0].EquippedRelicIds));
    }
}

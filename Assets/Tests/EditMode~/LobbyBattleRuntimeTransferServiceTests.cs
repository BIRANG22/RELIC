using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;

public class LobbyBattleRuntimeTransferServiceTests
{
    [Test]
    public void Transfer_CopiesCombatInventoryAndStartsWithEmptyBattleBag()
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
            Assert.That(battle.OwnedRelicIds, Is.EqualTo(new[] { "R_A" }));
            Assert.That(battle.SkillInventoryIds, Is.EqualTo(new[] { "S_A" }));
            Assert.That(battle.StartingSkillInventoryIds, Is.EqualTo(new[] { "S_A" }));
            Assert.That(battle.BagItemIds, Is.Empty);
            Assert.That(battle.OwnedRelicIds, Is.Not.SameAs(lobby.OwnedRelicIds));
            Assert.That(battle.BagItemIds, Is.Not.SameAs(lobby.BagItemIds));
            Assert.That(lobby.OwnedRelicIds, Is.Empty);
            Assert.That(lobby.SkillInventoryIds, Is.Empty);
            Assert.That(lobby.BagItemIds, Is.EqualTo(new[] { "I_A" }));
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
        Assert.That(lobby.CharacterLoadouts[0].EquippedRelicIds, Is.All.Null.Or.Empty);
        Assert.That(lobby.CharacterLoadouts[0].EquippedSkillIds[0], Is.EqualTo("S_A"));
    }

    [Test]
    public void ClearTransferredLobbyState_RemovesBattleStartOnlyDataWithoutClearingCharacterSettings()
    {
        var lobby = new LobbyRuntimeData
        {
            BlueDustium = 777,
            OwnedRelicIds = new List<string> { "R_A" },
            SkillInventoryIds = new List<string> { "S_A" },
            BagItemIds = new List<string> { "I_A" },
            CharacterLoadouts = new List<LobbyCharacterLoadoutData>
            {
                new()
                {
                    CharacterId = "Character_A",
                    EquippedRelicIds = new[] { "R_A", "R_B", null, null, null },
                    EquippedSkillIds = new[] { "S_Loadout", null, null, null }
                }
            },
            PendingCultureTankBattleStartEffects = new List<CultureTankBattleStartEffectRuntimeData>
            {
                new()
                {
                    SourceItemId = "Item_003",
                    EffectId = "E_Move_First_Attack_Power",
                    Value = 1,
                    Count = 1,
                    RemainingBattleStarts = 3
                }
            }
        };

        LobbyBattleRuntimeTransferService.ClearTransferredLobbyState(lobby);

        Assert.Multiple(() =>
        {
            Assert.That(lobby.OwnedRelicIds, Is.Empty);
            Assert.That(lobby.SkillInventoryIds, Is.Empty);
            Assert.That(lobby.PendingCultureTankBattleStartEffects, Is.Empty);
            Assert.That(lobby.CharacterLoadouts[0].EquippedRelicIds, Is.All.Null.Or.Empty);
            Assert.That(lobby.CharacterLoadouts[0].EquippedSkillIds[0], Is.EqualTo("S_Loadout"));
            Assert.That(lobby.BagItemIds, Is.EqualTo(new[] { "I_A" }));
            Assert.That(lobby.BlueDustium, Is.EqualTo(777));
        });
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;

public class SharedInventoryRuntimeContextTests
{
    [Test]
    public void LobbyContext_UsesLobbyInventoriesWithoutTouchingBattleRuntime()
    {
        var lobby = new LobbyRuntimeData
        {
            OwnedRelicIds = new List<string> { "LobbyRelic" },
            SkillInventoryIds = new List<string> { "LobbySkill" },
            BagItemIds = new List<string> { "LobbyItem" }
        };
        var battle = new BattleRuntimeData
        {
            OwnedRelicIds = new List<string> { "BattleRelic" },
            SkillInventoryIds = new List<string> { "BattleSkill" },
            BagItemIds = new List<string> { "BattleItem" }
        };

        IInventoryRuntimeContext context = InventoryRuntimeContext.ForLobby(lobby);

        context.OwnedRelicIds.Add("NewLobbyRelic");
        context.SkillInventoryIds.Add("NewLobbySkill");
        context.BagItemIds.Add("NewLobbyItem");

        Assert.That(lobby.OwnedRelicIds, Is.EqualTo(new[] { "LobbyRelic", "NewLobbyRelic" }));
        Assert.That(lobby.SkillInventoryIds, Is.EqualTo(new[] { "LobbySkill", "NewLobbySkill" }));
        Assert.That(lobby.BagItemIds, Is.EqualTo(new[] { "LobbyItem", "NewLobbyItem" }));
        Assert.That(battle.OwnedRelicIds, Is.EqualTo(new[] { "BattleRelic" }));
        Assert.That(battle.SkillInventoryIds, Is.EqualTo(new[] { "BattleSkill" }));
        Assert.That(battle.BagItemIds, Is.EqualTo(new[] { "BattleItem" }));
    }

    [Test]
    public void BattleContext_UsesBattleInventories()
    {
        var battle = new BattleRuntimeData();

        IInventoryRuntimeContext context = InventoryRuntimeContext.ForBattle(battle);

        context.OwnedRelicIds.Add("BattleRelic");
        context.SkillInventoryIds.Add("BattleSkill");
        context.BagItemIds.Add("BattleItem");

        Assert.That(battle.OwnedRelicIds, Is.EqualTo(new[] { "BattleRelic" }));
        Assert.That(battle.SkillInventoryIds, Is.EqualTo(new[] { "BattleSkill" }));
        Assert.That(battle.BagItemIds, Is.EqualTo(new[] { "BattleItem" }));
    }
}

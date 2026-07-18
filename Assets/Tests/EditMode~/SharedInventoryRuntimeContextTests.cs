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
            SkillInventoryIds = new List<string> { "LobbySkill" }
        };
        var battle = new BattleRuntimeData
        {
            OwnedRelicIds = new List<string> { "BattleRelic" },
            SkillInventoryIds = new List<string> { "BattleSkill" }
        };

        IInventoryRuntimeContext context = InventoryRuntimeContext.ForLobby(lobby);

        context.OwnedRelicIds.Add("NewLobbyRelic");
        context.SkillInventoryIds.Add("NewLobbySkill");

        Assert.That(lobby.OwnedRelicIds, Is.EqualTo(new[] { "LobbyRelic", "NewLobbyRelic" }));
        Assert.That(lobby.SkillInventoryIds, Is.EqualTo(new[] { "LobbySkill", "NewLobbySkill" }));
        Assert.That(battle.OwnedRelicIds, Is.EqualTo(new[] { "BattleRelic" }));
        Assert.That(battle.SkillInventoryIds, Is.EqualTo(new[] { "BattleSkill" }));
    }

    [Test]
    public void BattleContext_UsesBattleInventories()
    {
        var battle = new BattleRuntimeData();

        IInventoryRuntimeContext context = InventoryRuntimeContext.ForBattle(battle);

        context.OwnedRelicIds.Add("BattleRelic");
        context.SkillInventoryIds.Add("BattleSkill");

        Assert.That(battle.OwnedRelicIds, Is.EqualTo(new[] { "BattleRelic" }));
        Assert.That(battle.SkillInventoryIds, Is.EqualTo(new[] { "BattleSkill" }));
    }
}

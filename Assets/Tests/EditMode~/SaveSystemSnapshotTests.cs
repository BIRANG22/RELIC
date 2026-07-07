using NUnit.Framework;
using Relic.Gameplay.Data;

public class SaveSystemSnapshotTests
{
    [Test]
    public void CreateSaveDataSnapshot_PreservesPartyLoadoutInventoryRelicsAndMapProgress()
    {
        var partyStore = new PartyRuntimeStore();
        partyStore.SetSlot(0, "char_a", 4);
        partyStore.SetCurrentGridIndex(0, 11);

        var character = new CharacterRuntimeData
        {
            CharacterId = "char_a",
            EquippedSkillIds = new[] { "skill_passive", "skill_unique", "skill_free", "skill_ability" },
            EquippedRelicIds = new[] { "relic_a", "relic_b", "", "", "" }
        };

        var skill = new SkillRuntimeData
        {
            CharacterId = "char_a",
            SkillId = "skill_free",
            Level = 3,
            Exp = 14,
            IsUnlocked = true
        };

        var mapRuntime = new MapRuntimeData
        {
            SelectedChapterId = "chapter_1",
            CurrentStage = "stage_2",
            CurrentMapId = "map_elite_03",
            CurrentNodeIndex = 8,
            IsRunInitialized = true
        };
        mapRuntime.VisitedMapIds.Add("8");
        mapRuntime.ClearedMapIds.Add("7");

        var battleRuntime = new BattleRuntimeData
        {
            Remnant = 230,
            CurrentBattleCount = 5,
            CurrentRewardCount = 2,
            IsBattleRunInitialized = true
        };
        battleRuntime.SkillInventoryIds.Add("skill_inventory_a");
        battleRuntime.OwnedRelicIds.Add("relic_owned_a");
        battleRuntime.BagItemIds.Add("item_bag_a");

        GameSaveData saveData = SaveSystem.CreateSaveDataSnapshot(
            null,
            partyStore,
            new[] { character },
            new[] { skill },
            mapRuntime,
            battleRuntime,
            "Battle",
            GameMode.SingleStory);

        Assert.That(saveData.Party.Slots[0].CharacterId, Is.EqualTo("char_a"));
        Assert.That(saveData.Party.Slots[0].SpawnGridIndex, Is.EqualTo(4));
        Assert.That(saveData.Party.Slots[0].CurrentGridIndex, Is.EqualTo(11));
        Assert.That(saveData.Characters[0].EquippedSkillIds, Is.EqualTo(character.EquippedSkillIds));
        Assert.That(saveData.Characters[0].EquippedRelicIds, Is.EqualTo(character.EquippedRelicIds));
        Assert.That(saveData.Skills[0].SkillId, Is.EqualTo("skill_free"));
        Assert.That(saveData.Battle.SkillInventoryIds, Is.EqualTo(new[] { "skill_inventory_a" }));
        Assert.That(saveData.Battle.OwnedRelicIds, Is.EqualTo(new[] { "relic_owned_a" }));
        Assert.That(saveData.Battle.BagItemIds, Is.EqualTo(new[] { "item_bag_a" }));
        Assert.That(saveData.Map.CurrentStage, Is.EqualTo("stage_2"));
        Assert.That(saveData.Map.CurrentMapId, Is.EqualTo("map_elite_03"));
        Assert.That(saveData.Map.CurrentNodeIndex, Is.EqualTo(8));
        Assert.That(saveData.Map.CurrentSceneName, Is.EqualTo("Battle"));

        character.EquippedSkillIds[2] = "mutated_skill";
        character.EquippedRelicIds[0] = "mutated_relic";
        battleRuntime.SkillInventoryIds[0] = "mutated_inventory";
        mapRuntime.CurrentMapId = "mutated_map";

        Assert.That(saveData.Characters[0].EquippedSkillIds[2], Is.EqualTo("skill_free"));
        Assert.That(saveData.Characters[0].EquippedRelicIds[0], Is.EqualTo("relic_a"));
        Assert.That(saveData.Battle.SkillInventoryIds[0], Is.EqualTo("skill_inventory_a"));
        Assert.That(saveData.Map.CurrentMapId, Is.EqualTo("map_elite_03"));
    }
}

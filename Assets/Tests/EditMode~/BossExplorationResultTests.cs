using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public class BossExplorationResultTests
{
    [Test]
    public void StatisticsService_AccumulatesValuesByCharacterId()
    {
        BattleRuntimeData runtime = new();

        BattleRunStatisticsService.RecordDamageDealt(runtime, "Char_A", 12);
        BattleRunStatisticsService.RecordDamageDealt(runtime, "Char_A", 8);
        BattleRunStatisticsService.RecordDamageTaken(runtime, "Char_A", 7);
        BattleRunStatisticsService.RecordDeath(runtime, "Char_A");
        BattleRunStatisticsService.RecordKill(runtime, "Char_A");

        BattleRunCharacterStatisticsData stats = runtime.CharacterStatistics[0];
        Assert.That(stats.CharacterId, Is.EqualTo("Char_A"));
        Assert.That(stats.DamageDealt, Is.EqualTo(20));
        Assert.That(stats.DamageTaken, Is.EqualTo(7));
        Assert.That(stats.DeathCount, Is.EqualTo(1));
        Assert.That(stats.KillCount, Is.EqualTo(1));
    }

    [Test]
    public void ResultBuilder_IncludesOnlySkillsNotOwnedAtRunStart()
    {
        BattleRuntimeData runtime = new()
        {
            Remnant = 320,
            StartingSkillInventoryIds = new List<string> { "Skill_Start", "Skill_Kept" },
            SkillInventoryIds = new List<string> { "Skill_Kept", "Skill_New" },
            AcquiredSkillIds = new List<string> { "Skill_New", "Skill_EquippedNew" },
            OwnedRelicIds = new List<string> { "Relic_A" }
        };

        ExplorationResultData result = ExplorationResultBuilder.Build(runtime);

        Assert.That(result.Remnant, Is.EqualTo(320));
        Assert.That(result.RelicIds, Is.EqualTo(new[] { "Relic_A" }));
        Assert.That(result.NewSkillIds, Is.EqualTo(new[] { "Skill_New", "Skill_EquippedNew" }));
    }

    [Test]
    public void ResultBuilder_IncludesBagItemsForBaseCarryOver()
    {
        BattleRuntimeData runtime = new()
        {
            BagItemIds = new List<string> { "Item_A", " ", "Item_B", "Item_A" }
        };

        ExplorationResultData result = ExplorationResultBuilder.Build(runtime);

        Assert.That(result.BagItemIds, Is.EqualTo(new[] { "Item_A", "Item_B" }));
    }

    [Test]
    public void ResearchPolicy_ConvertsRemnantRelicsAndSkillsByRarity()
    {
        ResearchConversionBreakdown result = ResearchConversionPolicy.Calculate(
            321,
            new[] { RelicRarity.Common, RelicRarity.Rare, RelicRarity.Unique },
            new[] { SkillRarity.CoreCommon, SkillRarity.CoreEpic });

        Assert.That(result.RemnantBlue, Is.EqualTo(160));
        Assert.That(result.RelicBlue, Is.EqualTo(160));
        Assert.That(result.SkillBlue, Is.EqualTo(60));
        Assert.That(result.TotalBlue, Is.EqualTo(380));
    }

    [Test]
    public void PendingResearch_DefeatMultiplierHalvesConvertedReward()
    {
        PendingResearchResultData pending = ExplorationResearchService.CreatePending(
            new ExplorationResultData { Remnant = 321 },
            null,
            0.5f);

        Assert.That(pending.RemnantBlue, Is.EqualTo(80));
        Assert.That(pending.TotalBlue, Is.EqualTo(80));
    }

    [Test]
    public void PendingResearch_ApplyCreditsBlueOnlyOnce()
    {
        LobbyRuntimeData lobby = new() { BlueDustium = 100 };
        lobby.HasPendingResearchResult = true;
        lobby.PendingResearchResult = new PendingResearchResultData { TotalBlue = 75 };

        Assert.That(PendingResearchSettlementService.ApplyOnce(lobby), Is.True);
        Assert.That(PendingResearchSettlementService.ApplyOnce(lobby), Is.False);
        Assert.That(lobby.BlueDustium, Is.EqualTo(175));
    }

    [Test]
    public void PendingResearch_ApplyCarriesBagItemsWithoutExtraBlueConversion()
    {
        LobbyRuntimeData lobby = new()
        {
            BlueDustium = 100,
            BagItemIds = new List<string> { "Item_Old" },
            HasPendingResearchResult = true,
            PendingResearchResult = new PendingResearchResultData
            {
                TotalBlue = 75,
                ExplorationResult = new ExplorationResultData
                {
                    BagItemIds = new List<string> { "Item_New", "Item_Old" }
                }
            }
        };

        Assert.That(PendingResearchSettlementService.ApplyOnce(lobby), Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(lobby.BlueDustium, Is.EqualTo(175));
            Assert.That(lobby.BagItemIds, Is.EqualTo(new[] { "Item_Old", "Item_New" }));
        });
    }

    [Test]
    public void PendingResearch_EmptyDeserializedObjectWithoutPendingFlagIsIgnored()
    {
        LobbyRuntimeData lobby = new()
        {
            BlueDustium = 100,
            HasPendingResearchResult = false,
            PendingResearchResult = new PendingResearchResultData()
        };

        Assert.That(PendingResearchSettlementService.HasPending(lobby), Is.False);
        Assert.That(PendingResearchSettlementService.ApplyOnce(lobby), Is.False);
        Assert.That(lobby.BlueDustium, Is.EqualTo(100));
    }

    [Test]
    public void RewardPanel_WithCompletionCallbackKeepsBattlePanelForFollowupUi()
    {
        GameObject panelObject = new("BossRewardPanel");
        GameObject battlePanelObject = new("BattlePanel");
        GameObject mapPanelObject = new("MapPanel");

        try
        {
            BattleRewardPanelUI panel = panelObject.AddComponent<BattleRewardPanelUI>();
            SetPrivateField(panel, "battlePanel", battlePanelObject);
            SetPrivateField(panel, "mapPanel", mapPanelObject);

            battlePanelObject.SetActive(true);
            mapPanelObject.SetActive(false);

            bool completed = false;
            panel.Open(new List<BattleRewardData>(), () => completed = true);

            Assert.That(completed, Is.True);
            Assert.That(panelObject.activeSelf, Is.False);
            Assert.That(battlePanelObject.activeSelf, Is.True);
            Assert.That(mapPanelObject.activeSelf, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(panelObject);
            Object.DestroyImmediate(battlePanelObject);
            Object.DestroyImmediate(mapPanelObject);
        }
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, $"Missing field: {fieldName}");
        field.SetValue(target, value);
    }

}

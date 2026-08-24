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
        BattleRunStatisticsService.RecordBuffApplied(runtime, "Char_A", 9);
        BattleRunStatisticsService.RecordDeath(runtime, "Char_A");
        BattleRunStatisticsService.RecordKill(runtime, "Char_A");

        BattleRunCharacterStatisticsData stats = runtime.CharacterStatistics[0];
        Assert.That(stats.CharacterId, Is.EqualTo("Char_A"));
        Assert.That(stats.DamageDealt, Is.EqualTo(20));
        Assert.That(stats.DamageTaken, Is.EqualTo(7));
        Assert.That(stats.BuffApplied, Is.EqualTo(9));
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
    public void ResultBuilder_CreatesZeroStatisticsRowsFromLobbySnapshots()
    {
        BattleRuntimeData runtime = new()
        {
            LobbyLoadoutSnapshots = new List<BattleLobbyLoadoutSnapshotData>
            {
                new() { CharacterId = "Char_A" },
                new() { CharacterId = " Char_B " }
            }
        };

        ExplorationResultData result = ExplorationResultBuilder.Build(runtime);

        Assert.That(result.CharacterStatistics, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(result.CharacterStatistics[0].CharacterId, Is.EqualTo("Char_A"));
            Assert.That(result.CharacterStatistics[0].KillCount, Is.EqualTo(0));
            Assert.That(result.CharacterStatistics[0].DamageDealt, Is.EqualTo(0));
            Assert.That(result.CharacterStatistics[0].DamageTaken, Is.EqualTo(0));
            Assert.That(result.CharacterStatistics[0].BuffApplied, Is.EqualTo(0));
            Assert.That(result.CharacterStatistics[0].DeathCount, Is.EqualTo(0));
            Assert.That(result.CharacterStatistics[1].CharacterId, Is.EqualTo("Char_B"));
        });
    }

    [Test]
    public void ResultBuilder_MergesRecordedStatisticsIntoSnapshotRows()
    {
        BattleRuntimeData runtime = new()
        {
            LobbyLoadoutSnapshots = new List<BattleLobbyLoadoutSnapshotData>
            {
                new() { CharacterId = "Char_A" },
                new() { CharacterId = "Char_B" }
            },
            CharacterStatistics = new List<BattleRunCharacterStatisticsData>
            {
                new()
                {
                    CharacterId = "Char_B",
                    DamageDealt = 123,
                    DamageTaken = 45,
                    BuffApplied = 6,
                    DeathCount = 1,
                    KillCount = 2
                },
                new() { CharacterId = "Char_C", KillCount = 1 }
            }
        };

        ExplorationResultData result = ExplorationResultBuilder.Build(runtime);

        Assert.That(result.CharacterStatistics, Has.Count.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(result.CharacterStatistics[0].CharacterId, Is.EqualTo("Char_A"));
            Assert.That(result.CharacterStatistics[0].DamageDealt, Is.EqualTo(0));
            Assert.That(result.CharacterStatistics[1].CharacterId, Is.EqualTo("Char_B"));
            Assert.That(result.CharacterStatistics[1].DamageDealt, Is.EqualTo(123));
            Assert.That(result.CharacterStatistics[1].DamageTaken, Is.EqualTo(45));
            Assert.That(result.CharacterStatistics[1].BuffApplied, Is.EqualTo(6));
            Assert.That(result.CharacterStatistics[1].DeathCount, Is.EqualTo(1));
            Assert.That(result.CharacterStatistics[1].KillCount, Is.EqualTo(2));
            Assert.That(result.CharacterStatistics[2].CharacterId, Is.EqualTo("Char_C"));
            Assert.That(result.CharacterStatistics[2].KillCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void StageClearExperience_UsesThousandExperiencePerLevel()
    {
        Assert.Multiple(() =>
        {
            Assert.That(BattleStageClearExperienceService.GetRequiredExperienceForNextLevel(1), Is.EqualTo(1000));
            Assert.That(BattleStageClearExperienceService.GetRequiredExperienceForNextLevel(29), Is.EqualTo(1000));
            Assert.That(BattleStageClearExperienceService.GetRequiredExperienceForNextLevel(30), Is.EqualTo(0));
        });
    }

    [Test]
    public void StageClearExperience_CalculatesTableRewardPerCharacter()
    {
        CharacterRuntimeStore store = new();
        store.AddOrUpdate(new CharacterRuntimeData { CharacterId = "Char_A", Level = 1, Exp = 900 });
        store.AddOrUpdate(new CharacterRuntimeData { CharacterId = "Char_B", Level = 1, Exp = 900 });

        BattleRunCharacterStatisticsData[] statistics =
        {
            new()
            {
                CharacterId = "Char_A",
                DamageDealt = 12,
                DamageTaken = 14,
                BuffApplied = 11,
                KillCount = 2
            },
            new()
            {
                CharacterId = "Char_B",
                DeathCount = 1
            }
        };

        BattleStageClearExperienceContext context = new(2, 1, 1, 1);

        IReadOnlyDictionary<string, BattleStageClearExperiencePreview> result =
            BattleStageClearExperienceService.Apply(store, statistics, context);

        Assert.Multiple(() =>
        {
            Assert.That(result["Char_A"].ExperienceGained, Is.EqualTo(834));
            Assert.That(result["Char_B"].ExperienceGained, Is.EqualTo(445));
            Assert.That(store.Get("Char_A").Level, Is.EqualTo(2));
            Assert.That(store.Get("Char_A").Exp, Is.EqualTo(1734));
            Assert.That(store.Get("Char_B").Level, Is.EqualTo(2));
            Assert.That(store.Get("Char_B").Exp, Is.EqualTo(1345));
        });
    }

    [Test]
    public void StageClearExperience_TreatsCharacterExpAsCumulativeTotal()
    {
        CharacterRuntimeStore store = new();
        store.AddOrUpdate(new CharacterRuntimeData { CharacterId = "Char_A", Level = 2, Exp = 1000 });

        BattleRunCharacterStatisticsData[] statistics =
        {
            new() { CharacterId = "Char_A" }
        };

        BattleStageClearExperiencePreview preview =
            BattleStageClearExperienceService.Apply(
                store,
                statistics,
                new BattleStageClearExperienceContext(1, 0, 0, 0))["Char_A"];

        Assert.Multiple(() =>
        {
            Assert.That(preview.LevelAfter, Is.EqualTo(2));
            Assert.That(preview.ExperienceAfter, Is.EqualTo(1090));
            Assert.That(preview.ProgressAfter01, Is.EqualTo(0.09f).Within(0.0001f));
            Assert.That(store.Get("Char_A").Level, Is.EqualTo(2));
            Assert.That(store.Get("Char_A").Exp, Is.EqualTo(1090));
        });
    }

    [Test]
    public void StageClearExperience_MigratesLegacyLevelLocalExpToCumulative()
    {
        CharacterRuntimeStore store = new();
        store.AddOrUpdate(new CharacterRuntimeData { CharacterId = "Char_A", Level = 2, Exp = 345 });

        BattleRunCharacterStatisticsData[] statistics =
        {
            new() { CharacterId = "Char_A" }
        };

        BattleStageClearExperiencePreview preview =
            BattleStageClearExperienceService.Apply(
                store,
                statistics,
                new BattleStageClearExperienceContext(1, 0, 0, 0))["Char_A"];

        Assert.Multiple(() =>
        {
            Assert.That(preview.LevelAfter, Is.EqualTo(2));
            Assert.That(preview.ExperienceAfter, Is.EqualTo(1435));
            Assert.That(store.Get("Char_A").Exp, Is.EqualTo(1435));
        });
    }

    [Test]
    public void StageClearExperience_DoesNotApplyDuplicateCharacterRowsTwice()
    {
        CharacterRuntimeStore store = new();
        store.AddOrUpdate(new CharacterRuntimeData { CharacterId = "Char_A", Level = 1, Exp = 0 });

        BattleRunCharacterStatisticsData[] statistics =
        {
            new() { CharacterId = "Char_A" },
            new() { CharacterId = "Char_A" }
        };

        BattleStageClearExperienceService.Apply(
            store,
            statistics,
            new BattleStageClearExperienceContext(1, 0, 0, 0));

        Assert.That(store.Get("Char_A").Exp, Is.EqualTo(90));
    }

    [Test]
    public void StageClearExperience_FloorsStatBonusesByFive()
    {
        CharacterRuntimeStore store = new();
        store.AddOrUpdate(new CharacterRuntimeData { CharacterId = "Char_A", Level = 1, Exp = 0 });

        BattleRunCharacterStatisticsData[] statistics =
        {
            new()
            {
                CharacterId = "Char_A",
                DamageDealt = 9,
                DamageTaken = 9,
                BuffApplied = 9,
                KillCount = 1
            }
        };

        BattleStageClearExperiencePreview preview =
            BattleStageClearExperienceService.Apply(
                store,
                statistics,
                BattleStageClearExperienceContext.Empty)["Char_A"];

        Assert.That(preview.ExperienceGained, Is.EqualTo(22));
    }

    [Test]
    public void StageClearExperience_BuildsContextFromClearedMapRuntime()
    {
        MapRuntimeData runtime = new()
        {
            CurrentNodeIndex = 4,
            ClearedMapIds = new List<string> { "1", "2", "3" },
            GeneratedNodes = new List<GeneratedMapNodeData>
            {
                new() { NodeIndex = 1, Type = "Common" },
                new() { NodeIndex = 2, Type = "Elite" },
                new() { NodeIndex = 3, Type = "Event" },
                new() { NodeIndex = 4, Type = "Boss" }
            }
        };

        BattleStageClearExperienceContext context =
            BattleStageClearExperienceService.BuildContext(
                runtime,
                MapRuntimeProgressUtility.FindCurrentNode(runtime),
                defeat: false);

        Assert.Multiple(() =>
        {
            Assert.That(context.NormalBattleClearCount, Is.EqualTo(1));
            Assert.That(context.EliteBattleClearCount, Is.EqualTo(1));
            Assert.That(context.BossBattleClearCount, Is.EqualTo(1));
            Assert.That(context.EventClearCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void StageClearExperience_BuildsDefeatContextFromPreviouslyClearedRooms()
    {
        MapRuntimeData runtime = new()
        {
            CurrentNodeIndex = 4,
            ClearedMapIds = new List<string> { "1", "2", "3" },
            GeneratedNodes = new List<GeneratedMapNodeData>
            {
                new() { NodeIndex = 1, Type = "Common" },
                new() { NodeIndex = 2, Type = "Elite" },
                new() { NodeIndex = 3, Type = "Event" },
                new() { NodeIndex = 4, Type = "Boss" }
            }
        };

        BattleStageClearExperienceContext context =
            BattleStageClearExperienceService.BuildContext(
                runtime,
                MapRuntimeProgressUtility.FindCurrentNode(runtime),
                defeat: true);

        Assert.Multiple(() =>
        {
            Assert.That(context.NormalBattleClearCount, Is.EqualTo(1));
            Assert.That(context.EliteBattleClearCount, Is.EqualTo(1));
            Assert.That(context.BossBattleClearCount, Is.EqualTo(0));
            Assert.That(context.EventClearCount, Is.EqualTo(1));
        });
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

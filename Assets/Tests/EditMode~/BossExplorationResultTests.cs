using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;

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
    public void PendingResearch_ApplyCreditsBlueOnlyOnce()
    {
        LobbyRuntimeData lobby = new() { BlueDustium = 100 };
        lobby.PendingResearchResult = new PendingResearchResultData { TotalBlue = 75 };

        Assert.That(PendingResearchSettlementService.ApplyOnce(lobby), Is.True);
        Assert.That(PendingResearchSettlementService.ApplyOnce(lobby), Is.False);
        Assert.That(lobby.BlueDustium, Is.EqualTo(175));
    }
}

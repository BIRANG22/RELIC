using System.Collections.Generic;
using Relic.Gameplay.Data;

public static class ExplorationResearchService
{
    public static PendingResearchResultData CreatePending(
        ExplorationResultData result,
        DataManager dataManager)
    {
        result ??= new ExplorationResultData();
        List<RelicRarity> relicRarities = new();
        List<SkillRarity> skillRarities = new();

        for (int i = 0; i < result.RelicIds.Count; i++)
        {
            if (dataManager?.RelicDatabase != null &&
                dataManager.RelicDatabase.TryGet(result.RelicIds[i], out RelicData relic) &&
                RelicRarityUtility.TryParseChestRarity(relic.Rarity, out RelicRarity rarity))
            {
                relicRarities.Add(rarity);
            }
        }

        for (int i = 0; i < result.NewSkillIds.Count; i++)
        {
            if (dataManager?.SkillDatabase != null &&
                dataManager.SkillDatabase.TryGet(result.NewSkillIds[i], out SkillMasterData skill))
            {
                skillRarities.Add(skill.Rarity);
            }
        }

        ResearchConversionBreakdown conversion = ResearchConversionPolicy.Calculate(
            result.Remnant,
            relicRarities,
            skillRarities);

        return new PendingResearchResultData
        {
            ExplorationResult = result,
            RemnantBlue = conversion.RemnantBlue,
            RelicBlue = conversion.RelicBlue,
            SkillBlue = conversion.SkillBlue,
            TotalBlue = conversion.TotalBlue
        };
    }
}

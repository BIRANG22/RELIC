using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;

public static class ExplorationResearchService
{
    public static PendingResearchResultData CreatePending(
        ExplorationResultData result,
        DataManager dataManager,
        float rewardMultiplier = 1f)
    {
        result ??= new ExplorationResultData();
        rewardMultiplier = Mathf.Max(0f, rewardMultiplier);
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

        int remnantBlue = ScaleReward(conversion.RemnantBlue, rewardMultiplier);
        int relicBlue = ScaleReward(conversion.RelicBlue, rewardMultiplier);
        int skillBlue = ScaleReward(conversion.SkillBlue, rewardMultiplier);

        return new PendingResearchResultData
        {
            ExplorationResult = result,
            RemnantBlue = remnantBlue,
            RelicBlue = relicBlue,
            SkillBlue = skillBlue,
            TotalBlue = remnantBlue + relicBlue + skillBlue
        };
    }

    private static int ScaleReward(int value, float multiplier)
    {
        return Mathf.FloorToInt(Mathf.Max(0, value) * multiplier);
    }
}

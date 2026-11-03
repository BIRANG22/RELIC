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
        int remnantBlue = CalculateOneToOneBlueDustium(result.Remnant);

        return new PendingResearchResultData
        {
            ExplorationResult = result,
            RemnantBlue = remnantBlue,
            RelicBlue = 0,
            SkillBlue = 0,
            TotalBlue = remnantBlue
        };
    }

    public static int CalculateOneToOneBlueDustium(int redDustium)
    {
        return Mathf.Max(0, redDustium);
    }
}

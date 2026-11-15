using Relic.Gameplay.Data;

public static class BattleRelicExtractionService
{
    // Used by the reward UI and the actual extraction so the displayed amount is identical.
    public static int GetExtractionAmount(RelicData relic, int commonAmount, int rareAmount, int epicAmount, int uniqueAmount)
    {
        if (relic == null) return 0;
        RelicRarityUtility.TryParseChestRarity(relic.Rarity, out RelicRarity rarity);
        int amount = rarity switch
        {
            RelicRarity.Unique => uniqueAmount,
            RelicRarity.Epic => epicAmount,
            RelicRarity.Rare => rareAmount,
            _ => commonAmount
        };
        return System.Math.Max(0, amount);
    }

    public static bool TryExtract(
        BattleRuntimeStore runtimeStore,
        RelicDatabase relicDatabase,
        string relicId,
        int commonAmount,
        int rareAmount,
        int epicAmount,
        int uniqueAmount,
        out int extractedAmount)
    {
        extractedAmount = 0;
        if (runtimeStore == null || relicDatabase == null ||
            !relicDatabase.TryGet(relicId, out RelicData relic) || relic == null)
        {
            return false;
        }

        extractedAmount = GetExtractionAmount(relic, commonAmount, rareAmount, epicAmount, uniqueAmount);

        BattleRuntimeData runtime = runtimeStore.GetOrCreate();
        runtime.Remnant += extractedAmount;
        runtimeStore.Set(runtime);
        return true;
    }
}

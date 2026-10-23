using Relic.Gameplay.Data;

public static class BattleRelicExtractionService
{
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

        RelicRarityUtility.TryParseChestRarity(relic.Rarity, out RelicRarity rarity);
        extractedAmount = rarity switch
        {
            RelicRarity.Unique => uniqueAmount,
            RelicRarity.Epic => epicAmount,
            RelicRarity.Rare => rareAmount,
            _ => commonAmount
        };
        extractedAmount = System.Math.Max(0, extractedAmount);

        BattleRuntimeData runtime = runtimeStore.GetOrCreate();
        runtime.Remnant += extractedAmount;
        runtimeStore.Set(runtime);
        return true;
    }
}

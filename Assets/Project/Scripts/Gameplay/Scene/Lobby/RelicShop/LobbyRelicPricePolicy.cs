using Relic.Gameplay.Data;

public static class LobbyRelicPricePolicy
{
    public static bool TryGetPrice(string rarityText, out int price)
    {
        price = 0;

        if (!RelicRarityUtility.TryParseChestRarity(rarityText, out RelicRarity rarity))
            return false;

        price = rarity switch
        {
            RelicRarity.Common => 100,
            RelicRarity.Uncommon => 200,
            RelicRarity.Rare => 300,
            RelicRarity.Unique => 500,
            _ => 0
        };

        return price > 0;
    }
}

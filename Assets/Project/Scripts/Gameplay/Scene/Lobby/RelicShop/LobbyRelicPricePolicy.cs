using Relic.Gameplay.Data;

public static class LobbyRelicPricePolicy
{
    public static bool TryGetPrice(RelicData relic, out int price)
    {
        price = relic != null ? relic.BlueDustiumCost : 0;
        return price > 0;
    }
}

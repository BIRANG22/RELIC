using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;

public static class LobbyRelicShopPurchaseLimit
{
    public static bool HasPurchasedOffer(LobbyRuntimeData runtime)
    {
        if (runtime?.RelicOfferIds == null || runtime.OwnedRelicIds == null)
            return false;

        for (int i = 0; i < runtime.RelicOfferIds.Count; i++)
        {
            string offerId = runtime.RelicOfferIds[i];
            if (Contains(runtime.OwnedRelicIds, offerId))
                return true;
        }

        return false;
    }

    private static bool Contains(IEnumerable<string> ids, string target)
    {
        if (ids == null || string.IsNullOrWhiteSpace(target))
            return false;

        string normalizedTarget = target.Trim();
        foreach (string id in ids)
        {
            if (string.Equals(id?.Trim(), normalizedTarget, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}

using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;

public static class LobbyRelicShopPurchaseLimit
{
    public static bool HasPurchasedOffer(LobbyRuntimeData runtime)
    {
        if (runtime == null)
            return false;

        if (runtime.RelicShopPurchaseLocked)
            return true;

        // 이전 저장 데이터 호환:
        // 새 잠금 필드가 생기기 전에 구매한 세이브는 현재 제안 유물이 OwnedRelicIds에 남아 있으면
        // 이미 구매한 상태로 간주하고 새 잠금값으로 승격합니다.
        if (HasLegacyPurchasedOffer(runtime))
        {
            runtime.RelicShopPurchaseLocked = true;
            return true;
        }

        return false;
    }

    public static void LockAfterPurchase(LobbyRuntimeData runtime)
    {
        if (runtime == null)
            return;

        runtime.RelicShopPurchaseLocked = true;
    }

    public static void ResetAfterExploration(LobbyRuntimeData runtime)
    {
        if (runtime == null)
            return;

        runtime.RelicShopPurchaseLocked = false;
        runtime.RelicRefreshCount = 0;
        runtime.RelicOfferSeed = 0;
        runtime.RelicOfferIds?.Clear();
    }

    private static bool HasLegacyPurchasedOffer(LobbyRuntimeData runtime)
    {
        if (runtime?.RelicOfferIds == null || runtime.OwnedRelicIds == null)
            return false;

        for (int i = 0; i < runtime.RelicOfferIds.Count; i++)
        {
            if (Contains(runtime.OwnedRelicIds, runtime.RelicOfferIds[i]))
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

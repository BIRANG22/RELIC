using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;

public static class LobbyRelicRefreshPricePolicy
{
    public const int BasePrice = 50;
    public const int InitialIncrease = 15;
    public const int IncreaseStep = 5;

    // 50 -> 65 -> 85 -> 110 -> 140 -> 175 ...
    public static int GetPrice(int successfulRefreshCount)
    {
        int count = Math.Max(0, successfulRefreshCount);
        long extra = (long)InitialIncrease * count
                   + (long)IncreaseStep * count * (count - 1) / 2;
        long price = BasePrice + extra;
        return price >= int.MaxValue ? int.MaxValue : (int)price;
    }
}

public enum LobbyRelicRefreshFailure
{
    None,
    InvalidRuntime,
    AllOffersPurchased,
    PurchaseLimitReached,
    InsufficientBlueDustium,
    NotEnoughCandidates
}

public readonly struct LobbyRelicRefreshResult
{
    public LobbyRelicRefreshResult(bool succeeded, int price, LobbyRelicRefreshFailure failure)
    {
        Succeeded = succeeded;
        Price = price;
        Failure = failure;
    }

    public bool Succeeded { get; }
    public int Price { get; }
    public LobbyRelicRefreshFailure Failure { get; }
}

public sealed class LobbyRelicRefreshService
{
    private readonly RelicDatabase relicDatabase;
    private readonly ILobbyRelicShopRandom random;

    public LobbyRelicRefreshService(RelicDatabase relicDatabase, ILobbyRelicShopRandom random)
    {
        this.relicDatabase = relicDatabase;
        this.random = random;
    }

    public LobbyRelicRefreshResult Execute(LobbyRuntimeData runtime, int nextSeed)
    {
        int refreshCount = Math.Max(0, runtime?.RelicRefreshCount ?? 0);
        int price = LobbyRelicRefreshPricePolicy.GetPrice(refreshCount);

        if (runtime == null || relicDatabase == null || random == null || runtime.RelicOfferIds == null)
            return Fail(price, LobbyRelicRefreshFailure.InvalidRuntime);

        var refreshableSlotIndices = new List<int>();
        for (int i = 0; i < runtime.RelicOfferIds.Count; i++)
        {
            string offerId = runtime.RelicOfferIds[i];
            if (!string.IsNullOrWhiteSpace(offerId) && !Contains(runtime.OwnedRelicIds, offerId))
                refreshableSlotIndices.Add(i);
        }

        if (refreshableSlotIndices.Count == 0)
            return Fail(price, LobbyRelicRefreshFailure.AllOffersPurchased);

        if (runtime.BlueDustium < price)
            return Fail(price, LobbyRelicRefreshFailure.InsufficientBlueDustium);

        var excludedIds = new List<string>();
        if (runtime.OwnedRelicIds != null)
            excludedIds.AddRange(runtime.OwnedRelicIds);

        // 현재 진열 중인 유물은 구매 여부와 관계없이 새 리롤 후보에서 제외합니다.
        // 구매한 슬롯은 그대로 유지하고, 미구매 슬롯만 새 유물로 교체합니다.
        for (int i = 0; i < runtime.RelicOfferIds.Count; i++)
        {
            string offerId = runtime.RelicOfferIds[i];
            if (!string.IsNullOrWhiteSpace(offerId))
                excludedIds.Add(offerId.Trim());
        }

        IReadOnlyList<LobbyRelicOffer> replacements = new LobbyRelicOfferService(random).BuildOffers(
            relicDatabase.GetAll(), excludedIds, refreshableSlotIndices.Count);
        if (replacements.Count < refreshableSlotIndices.Count)
            return Fail(price, LobbyRelicRefreshFailure.NotEnoughCandidates);

        for (int i = 0; i < refreshableSlotIndices.Count; i++)
            runtime.RelicOfferIds[refreshableSlotIndices[i]] = replacements[i].RelicId;

        runtime.BlueDustium -= price;
        runtime.RelicRefreshCount = refreshCount + 1;
        runtime.RelicOfferSeed = nextSeed == 0 ? 1 : nextSeed;

        return new LobbyRelicRefreshResult(true, price, LobbyRelicRefreshFailure.None);
    }

    public static bool AreAllOffersPurchased(LobbyRuntimeData runtime)
    {
        if (runtime?.RelicOfferIds == null || runtime.RelicOfferIds.Count == 0)
            return true;

        for (int i = 0; i < runtime.RelicOfferIds.Count; i++)
        {
            string offerId = runtime.RelicOfferIds[i];
            if (!string.IsNullOrWhiteSpace(offerId) && !Contains(runtime.OwnedRelicIds, offerId))
                return false;
        }

        return true;
    }

    private static bool Contains(IEnumerable<string> ids, string target)
    {
        if (ids == null || string.IsNullOrWhiteSpace(target))
            return false;

        foreach (string id in ids)
        {
            if (string.Equals(id?.Trim(), target.Trim(), StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static LobbyRelicRefreshResult Fail(int price, LobbyRelicRefreshFailure failure)
    {
        return new LobbyRelicRefreshResult(false, price, failure);
    }
}

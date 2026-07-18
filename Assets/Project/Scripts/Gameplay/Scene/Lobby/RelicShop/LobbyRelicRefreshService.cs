using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;

public static class LobbyRelicRefreshPricePolicy
{
    public const int BasePrice = 50;
    public const int PriceIncrease = 25;

    public static int GetPrice(int successfulRefreshCount)
    {
        long count = Math.Max(0, successfulRefreshCount);
        return (int)Math.Min(int.MaxValue, BasePrice + count * PriceIncrease);
    }
}

public enum LobbyRelicRefreshFailure
{
    None,
    InvalidRuntime,
    AllOffersPurchased,
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
        int price = LobbyRelicRefreshPricePolicy.GetPrice(runtime?.RelicRefreshCount ?? 0);
        if (runtime == null || relicDatabase == null || random == null || runtime.RelicOfferIds == null)
            return Fail(price, LobbyRelicRefreshFailure.InvalidRuntime);

        var refreshIndices = new List<int>();
        var excludedIds = new List<string>();
        if (runtime.OwnedRelicIds != null)
            excludedIds.AddRange(runtime.OwnedRelicIds);

        for (int i = 0; i < runtime.RelicOfferIds.Count; i++)
        {
            string offerId = runtime.RelicOfferIds[i];
            if (Contains(runtime.OwnedRelicIds, offerId))
                continue;
            refreshIndices.Add(i);
            if (!string.IsNullOrWhiteSpace(offerId))
                excludedIds.Add(offerId.Trim());
        }

        if (refreshIndices.Count == 0)
            return Fail(price, LobbyRelicRefreshFailure.AllOffersPurchased);
        if (runtime.BlueDustium < price)
            return Fail(price, LobbyRelicRefreshFailure.InsufficientBlueDustium);

        IReadOnlyList<LobbyRelicOffer> replacements = new LobbyRelicOfferService(random).BuildOffers(
            relicDatabase.GetAll(), excludedIds, refreshIndices.Count);
        if (replacements.Count < refreshIndices.Count)
            return Fail(price, LobbyRelicRefreshFailure.NotEnoughCandidates);

        for (int i = 0; i < refreshIndices.Count; i++)
            runtime.RelicOfferIds[refreshIndices[i]] = replacements[i].RelicId;

        runtime.BlueDustium -= price;
        runtime.RelicRefreshCount++;
        runtime.RelicOfferSeed = nextSeed == 0 ? 1 : nextSeed;
        return new LobbyRelicRefreshResult(true, price, LobbyRelicRefreshFailure.None);
    }

    public static bool AreAllOffersPurchased(LobbyRuntimeData runtime)
    {
        if (runtime?.RelicOfferIds == null || runtime.RelicOfferIds.Count == 0)
            return true;
        for (int i = 0; i < runtime.RelicOfferIds.Count; i++)
            if (!Contains(runtime.OwnedRelicIds, runtime.RelicOfferIds[i]))
                return false;
        return true;
    }

    private static bool Contains(IEnumerable<string> ids, string target)
    {
        if (ids == null || string.IsNullOrWhiteSpace(target))
            return false;
        foreach (string id in ids)
            if (string.Equals(id?.Trim(), target.Trim(), StringComparison.Ordinal))
                return true;
        return false;
    }

    private static LobbyRelicRefreshResult Fail(int price, LobbyRelicRefreshFailure failure)
    {
        return new LobbyRelicRefreshResult(false, price, failure);
    }
}

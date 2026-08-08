using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;

public enum LobbyRelicPurchaseFailure
{
    None,
    InvalidRequest,
    RelicNotFound,
    NotActiveRelic,
    UnknownRarity,
    AlreadyOwned,
    PurchaseLimitReached,
    InsufficientBlueDustium
}

public readonly struct LobbyRelicPurchaseCommand
{
    public LobbyRelicPurchaseCommand(string relicId)
    {
        RelicId = relicId;
    }

    public string RelicId { get; }
}

public readonly struct LobbyRelicPurchaseResult
{
    public LobbyRelicPurchaseResult(
        bool succeeded,
        string relicId,
        int price,
        LobbyRelicPurchaseFailure failure)
    {
        Succeeded = succeeded;
        RelicId = relicId;
        Price = price;
        Failure = failure;
    }

    public bool Succeeded { get; }
    public string RelicId { get; }
    public int Price { get; }
    public LobbyRelicPurchaseFailure Failure { get; }
}

public sealed class LobbyRelicPurchaseService
{
    private readonly RelicDatabase relicDatabase;

    public LobbyRelicPurchaseService(RelicDatabase relicDatabase)
    {
        this.relicDatabase = relicDatabase;
    }

    public LobbyRelicPurchaseResult Execute(
        LobbyRelicPurchaseCommand command,
        LobbyRuntimeData runtime)
    {
        string relicId = command.RelicId?.Trim();
        if (runtime == null || string.IsNullOrEmpty(relicId))
            return Fail(relicId, LobbyRelicPurchaseFailure.InvalidRequest);

        if (relicDatabase == null || !relicDatabase.TryGet(relicId, out RelicData relic))
            return Fail(relicId, LobbyRelicPurchaseFailure.RelicNotFound);

        if (LobbyRelicShopPurchaseLimit.HasPurchasedOffer(runtime))
            return Fail(relicId, LobbyRelicPurchaseFailure.PurchaseLimitReached);

        if (!LobbyRelicPricePolicy.TryGetPrice(relic.Rarity, out int price))
            return Fail(relicId, LobbyRelicPurchaseFailure.UnknownRarity);

        runtime.OwnedRelicIds ??= new List<string>();
        if (Contains(runtime.OwnedRelicIds, relicId))
            return Fail(relicId, LobbyRelicPurchaseFailure.AlreadyOwned, price);

        if (runtime.BlueDustium < price)
            return Fail(relicId, LobbyRelicPurchaseFailure.InsufficientBlueDustium, price);

        runtime.BlueDustium -= price;
        runtime.OwnedRelicIds.Add(relicId);

        return new LobbyRelicPurchaseResult(true, relicId, price, LobbyRelicPurchaseFailure.None);
    }

    private static bool Contains(IEnumerable<string> ids, string target)
    {
        foreach (string id in ids)
        {
            if (string.Equals(id?.Trim(), target, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static LobbyRelicPurchaseResult Fail(
        string relicId,
        LobbyRelicPurchaseFailure failure,
        int price = 0)
    {
        return new LobbyRelicPurchaseResult(false, relicId, price, failure);
    }
}

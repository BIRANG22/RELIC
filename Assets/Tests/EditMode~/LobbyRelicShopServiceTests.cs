using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;

public class LobbyRelicShopServiceTests
{
    [TestCase("Common", 100)]
    [TestCase("Uncommon", 200)]
    [TestCase("Rare", 300)]
    [TestCase("Unique", 500)]
    public void PricePolicy_ReturnsConfiguredRarityPrice(string rarity, int expected)
    {
        Assert.That(LobbyRelicPricePolicy.TryGetPrice(rarity, out int price), Is.True);
        Assert.That(price, Is.EqualTo(expected));
    }

    [Test]
    public void BuildOffers_ExcludesPassiveOwnedAndDuplicateRelics()
    {
        RelicData owned = CreateRelic("Owned", "Active", "Common");
        RelicData activeA = CreateRelic("A", "Active", "Common");
        RelicData activeB = CreateRelic("B", "Active", "Rare");
        RelicData passive = CreateRelic("Passive", "Passive", "Unique");
        var random = new FirstIndexRandom();
        var service = new LobbyRelicOfferService(random);

        IReadOnlyList<LobbyRelicOffer> offers = service.BuildOffers(
            new[] { owned, activeA, activeB, passive },
            new[] { "Owned" },
            3);

        Assert.That(offers, Has.Count.EqualTo(2));
        Assert.That(offers[0].RelicId, Is.EqualTo("A"));
        Assert.That(offers[1].RelicId, Is.EqualTo("B"));
    }

    [Test]
    public void Purchase_ValidOffer_DeductsCurrencyAndAddsOwnedRelic()
    {
        RelicDatabase database = CreateDatabase(CreateRelic("A", "Active", "Common"));
        var runtime = new LobbyRuntimeData { BlueDustium = 999 };
        var service = new LobbyRelicPurchaseService(database);

        LobbyRelicPurchaseResult result = service.Execute(
            new LobbyRelicPurchaseCommand("A"), runtime);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(runtime.BlueDustium, Is.EqualTo(899));
            Assert.That(runtime.OwnedRelicIds, Does.Contain("A"));
        });
    }

    [Test]
    public void Purchase_InsufficientBalance_DoesNotMutateRuntime()
    {
        RelicDatabase database = CreateDatabase(CreateRelic("A", "Active", "Common"));
        var runtime = new LobbyRuntimeData { BlueDustium = 99 };
        var service = new LobbyRelicPurchaseService(database);

        LobbyRelicPurchaseResult result = service.Execute(
            new LobbyRelicPurchaseCommand("A"), runtime);

        Assert.That(result.Failure, Is.EqualTo(LobbyRelicPurchaseFailure.InsufficientBlueDustium));
        Assert.That(runtime.BlueDustium, Is.EqualTo(99));
        Assert.That(runtime.OwnedRelicIds, Is.Empty);
    }

    [Test]
    public void Purchase_OwnedRelic_DoesNotChargeAgain()
    {
        RelicDatabase database = CreateDatabase(CreateRelic("A", "Active", "Common"));
        var runtime = new LobbyRuntimeData
        {
            BlueDustium = 999,
            OwnedRelicIds = new List<string> { "A" }
        };
        var service = new LobbyRelicPurchaseService(database);

        LobbyRelicPurchaseResult result = service.Execute(
            new LobbyRelicPurchaseCommand("A"), runtime);

        Assert.That(result.Failure, Is.EqualTo(LobbyRelicPurchaseFailure.AlreadyOwned));
        Assert.That(runtime.BlueDustium, Is.EqualTo(999));
        Assert.That(runtime.OwnedRelicIds, Has.Count.EqualTo(1));
    }

    private static RelicData CreateRelic(string id, string type, string rarity)
    {
        return new RelicData { FragmentId = id, Type = type, Rarity = rarity };
    }

    private static RelicDatabase CreateDatabase(params RelicData[] relics)
    {
        var database = new RelicDatabase();
        database.Initialize(relics);
        return database;
    }

    private sealed class FirstIndexRandom : ILobbyRelicShopRandom
    {
        public int NextIndex(int exclusiveMax) => 0;
    }
}

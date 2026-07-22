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
    public void BuildOffers_IncludesActiveAndPassiveRelics_WhileExcludingOwnedRelics()
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

        Assert.That(offers, Has.Count.EqualTo(3));
        Assert.That(offers[0].RelicId, Is.EqualTo("A"));
        Assert.That(offers[1].RelicId, Is.EqualTo("B"));
        Assert.That(offers[2].RelicId, Is.EqualTo("Passive"));
    }

    [Test]
    public void Purchase_ValidOffer_DeductsCurrencyAndAddsOwnedRelic()
    {
        RelicDatabase database = CreateDatabase(CreateRelic("A", "Active", "Common"));
        var runtime = new LobbyRuntimeData { BlueDustium = 999 };
        var service = new LobbyRelicPurchaseService(database);

        LobbyRelicPurchaseResult result = service.Execute(
            new LobbyRelicPurchaseCommand("A"), runtime);

        Assert.That(result.Succeeded, Is.True);
        Assert.That(runtime.BlueDustium, Is.EqualTo(899));
        Assert.That(runtime.OwnedRelicIds, Does.Contain("A"));
    }

    [Test]
    public void Purchase_PassiveRelic_DeductsCurrencyAndAddsOwnedRelic()
    {
        RelicDatabase database = CreateDatabase(CreateRelic("Passive", "Passive", "Uncommon"));
        var runtime = new LobbyRuntimeData { BlueDustium = 999 };
        var service = new LobbyRelicPurchaseService(database);

        LobbyRelicPurchaseResult result = service.Execute(
            new LobbyRelicPurchaseCommand("Passive"), runtime);

        Assert.That(result.Succeeded, Is.True);
        Assert.That(runtime.BlueDustium, Is.EqualTo(799));
        Assert.That(runtime.OwnedRelicIds, Does.Contain("Passive"));
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

    [TestCase(0, 50)]
    [TestCase(1, 75)]
    [TestCase(2, 100)]
    [TestCase(3, 125)]
    public void RefreshPrice_IncreasesByTwentyFive(int count, int expected)
    {
        Assert.That(LobbyRelicRefreshPricePolicy.GetPrice(count), Is.EqualTo(expected));
    }

    [Test]
    public void Refresh_KeepsPurchasedSlotAndReplacesOnlyUnpurchasedSlots()
    {
        RelicDatabase database = CreateDatabase(
            CreateRelic("A", "Active", "Common"),
            CreateRelic("B", "Active", "Common"),
            CreateRelic("C", "Active", "Common"),
            CreateRelic("D", "Active", "Common"),
            CreateRelic("E", "Active", "Common"));
        var runtime = new LobbyRuntimeData
        {
            BlueDustium = 999,
            OwnedRelicIds = new List<string> { "C" },
            RelicOfferIds = new List<string> { "A", "B", "C" }
        };

        LobbyRelicRefreshResult result = new LobbyRelicRefreshService(
            database, new FirstIndexRandom()).Execute(runtime, 123);

        Assert.That(result.Succeeded, Is.True);
        Assert.That(runtime.RelicOfferIds[2], Is.EqualTo("C"));
        Assert.That(runtime.RelicOfferIds[0], Is.Not.EqualTo("A"));
        Assert.That(runtime.RelicOfferIds[1], Is.Not.EqualTo("B"));
        Assert.That(runtime.BlueDustium, Is.EqualTo(949));
        Assert.That(runtime.RelicRefreshCount, Is.EqualTo(1));
    }

    [Test]
    public void Refresh_AllOffersPurchased_DoesNotChargeOrChangeOffers()
    {
        RelicDatabase database = CreateDatabase(
            CreateRelic("A", "Active", "Common"),
            CreateRelic("B", "Active", "Common"),
            CreateRelic("C", "Active", "Common"));
        var runtime = new LobbyRuntimeData
        {
            BlueDustium = 999,
            OwnedRelicIds = new List<string> { "A", "B", "C" },
            RelicOfferIds = new List<string> { "A", "B", "C" }
        };

        LobbyRelicRefreshResult result = new LobbyRelicRefreshService(
            database, new FirstIndexRandom()).Execute(runtime, 123);

        Assert.That(result.Failure, Is.EqualTo(LobbyRelicRefreshFailure.AllOffersPurchased));
        Assert.That(runtime.BlueDustium, Is.EqualTo(999));
        Assert.That(runtime.RelicOfferIds, Is.EqualTo(new[] { "A", "B", "C" }));
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

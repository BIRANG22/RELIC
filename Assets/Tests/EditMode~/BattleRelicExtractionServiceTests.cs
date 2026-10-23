using NUnit.Framework;
using Relic.Gameplay.Data;

public sealed class BattleRelicExtractionServiceTests
{
    [Test]
    public void TryExtract_KnownRelic_AddsConfiguredAmountExactlyOnce()
    {
        var database = new RelicDatabase();
        database.Initialize(new[]
        {
            new RelicData { FragmentId = "Relic_Epic", Rarity = "Epic" }
        });
        var store = new BattleRuntimeStore();
        store.Set(new BattleRuntimeData { IsBattleRunInitialized = true, Remnant = 7 });

        bool extracted = BattleRelicExtractionService.TryExtract(
            store,
            database,
            "Relic_Epic",
            commonAmount: 10,
            rareAmount: 20,
            epicAmount: 30,
            uniqueAmount: 50,
            out int amount);

        Assert.That(extracted, Is.True);
        Assert.That(amount, Is.EqualTo(30));
        Assert.That(store.Get().Remnant, Is.EqualTo(37));
    }

    [Test]
    public void TryExtract_UnknownRelic_DoesNotChangeRuntime()
    {
        var database = new RelicDatabase();
        database.Initialize(null);
        var store = new BattleRuntimeStore();
        store.Set(new BattleRuntimeData { IsBattleRunInitialized = true, Remnant = 7 });

        bool extracted = BattleRelicExtractionService.TryExtract(
            store,
            database,
            "Missing",
            10,
            20,
            30,
            50,
            out int amount);

        Assert.That(extracted, Is.False);
        Assert.That(amount, Is.Zero);
        Assert.That(store.Get().Remnant, Is.EqualTo(7));
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;

public sealed class CultureTankResearchServiceTests
{
    [Test]
    public void TryPlaceIngredient_RemovesOneBagItemAndStoresStableSlotId()
    {
        LobbyRuntimeData lobby = new() { BagItemIds = new List<string> { "Item_001", "Item_002", "Item_001" } };

        bool placed = CultureTankResearchService.TryPlaceIngredient(
            lobby, "CultureTank1", "Item_001", out string error);

        Assert.That(placed, Is.True, error);
        Assert.That(lobby.BagItemIds, Is.EqualTo(new[] { "Item_002", "Item_001" }));
        Assert.That(CultureTankResearchService.TryGetTank(lobby, "CultureTank1", out CultureTankResearchRuntimeData slot), Is.True);
        Assert.That(slot.ItemId, Is.EqualTo("Item_001"));
    }

    [Test]
    public void TryRemoveIngredient_ReturnsItemToBag()
    {
        LobbyRuntimeData lobby = NewLobby();
        lobby.CultureTankResearches.Add(new CultureTankResearchRuntimeData { TankId = "CultureTank1", ItemId = "Item_001" });

        bool removed = CultureTankResearchService.TryRemoveIngredient(lobby, "CultureTank1", out string error);

        Assert.That(removed, Is.True, error);
        Assert.That(lobby.BagItemIds, Is.EqualTo(new[] { "Item_001" }));
        Assert.That(lobby.CultureTankResearches, Is.Empty);
    }

    [Test]
    public void NormalizeCombinationKey_IgnoresSlotOrder()
    {
        Assert.That(CultureTankCombinationDatabase.NormalizeKey("C", "A", "B"), Is.EqualTo("ABC"));
        Assert.That(CultureTankCombinationDatabase.NormalizeKey("B", "A", "A"), Is.EqualTo("AAB"));
    }

    [Test]
    public void TryCombine_ConsumesThreeSlotsAndStoresCompletionId()
    {
        LobbyRuntimeData lobby = CreateFilledLobby();
        ItemDatabase items = CreateItemDatabase();
        CultureTankCombinationDatabase recipes = CultureTankCombinationDatabase.CreateRuntime(new[]
        {
            new CultureTankCombinationEntry
            {
                CombinationId = "Culture_ABC", TypeA = "A", TypeB = "B", TypeC = "C",
                EffectId = "E_Move_First_Attack_Power", ValueRate = 1, CountRate = 1
            }
        });

        bool combined = CultureTankResearchService.TryCombine(lobby, items, recipes, out string combinationId, out string error);

        Assert.That(combined, Is.True, error);
        Assert.That(combinationId, Is.EqualTo("Culture_ABC"));
        Assert.That(lobby.CultureTankResearches, Is.Empty);
        Assert.That(lobby.CompletedCultureTankCombinationId, Is.EqualTo("Culture_ABC"));
    }

    [Test]
    public void TryCombine_MissingRecipeDoesNotConsumeIngredients()
    {
        LobbyRuntimeData lobby = CreateFilledLobby();
        ItemDatabase items = CreateItemDatabase();
        CultureTankCombinationDatabase recipes = CultureTankCombinationDatabase.CreateRuntime(new CultureTankCombinationEntry[0]);

        bool combined = CultureTankResearchService.TryCombine(lobby, items, recipes, out _, out _);

        Assert.That(combined, Is.False);
        Assert.That(lobby.CultureTankResearches, Has.Count.EqualTo(3));
        Assert.That(lobby.CompletedCultureTankCombinationId, Is.Empty);
    }

    [Test]
    public void TryClaimCompletedCombination_UsesRecipeRatesAndClearsCompletion()
    {
        LobbyRuntimeData lobby = NewLobby();
        lobby.CompletedCultureTankCombinationId = "Culture_ABC";
        CultureTankCombinationDatabase recipes = CultureTankCombinationDatabase.CreateRuntime(new[]
        {
            new CultureTankCombinationEntry
            {
                CombinationId = "Culture_ABC", TypeA = "A", TypeB = "B", TypeC = "C",
                EffectId = "E_Move_First_Attack_Power", ValueRate = 2, CountRate = 3,
                RemainingBattleStarts = 4
            }
        });

        bool claimed = CultureTankResearchService.TryClaimCompletedCombination(
            lobby, recipes, out CultureTankBattleStartEffectRuntimeData effect, out string error);

        Assert.That(claimed, Is.True, error);
        Assert.That(effect.SourceItemId, Is.EqualTo("Culture_ABC"));
        Assert.That(effect.EffectId, Is.EqualTo("E_Move_First_Attack_Power"));
        Assert.That(effect.Value, Is.EqualTo(2));
        Assert.That(effect.Count, Is.EqualTo(3));
        Assert.That(effect.RemainingBattleStarts, Is.EqualTo(4));
        Assert.That(lobby.CompletedCultureTankCombinationId, Is.Empty);
        Assert.That(lobby.PendingCultureTankBattleStartEffects, Has.Count.EqualTo(1));
    }

    private static LobbyRuntimeData CreateFilledLobby()
    {
        LobbyRuntimeData lobby = NewLobby();
        lobby.CultureTankResearches.Add(new CultureTankResearchRuntimeData { TankId = "CultureTank1", ItemId = "Item_003" });
        lobby.CultureTankResearches.Add(new CultureTankResearchRuntimeData { TankId = "CultureTank2", ItemId = "Item_001" });
        lobby.CultureTankResearches.Add(new CultureTankResearchRuntimeData { TankId = "CultureTank3", ItemId = "Item_002" });
        return lobby;
    }

    private static LobbyRuntimeData NewLobby() => new()
    {
        CultureTankCombinationSchemaVersion = CultureTankResearchService.CurrentSchemaVersion
    };

    private static ItemDatabase CreateItemDatabase()
    {
        ItemDatabase database = new();
        database.Initialize(new[]
        {
            new ItemData { ItemId = "Item_001", CultureType = "A" },
            new ItemData { ItemId = "Item_002", CultureType = "B" },
            new ItemData { ItemId = "Item_003", CultureType = "C" }
        });
        return database;
    }
}

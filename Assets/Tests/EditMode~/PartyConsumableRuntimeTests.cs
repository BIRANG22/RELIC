using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;

public sealed class PartyConsumableRuntimeTests
{
    [Test]
    public void NormalizeLobby_CreatesExactlyFourSharedSlots()
    {
        LobbyRuntimeData lobby = new() { EquippedConsumableIds = null };

        PartyConsumableRuntime.NormalizeLobby(lobby);

        Assert.That(lobby.EquippedConsumableIds, Is.Not.Null);
        Assert.That(lobby.EquippedConsumableIds.Length, Is.EqualTo(4));
    }

    [Test]
    public void TryEquip_AllowsDuplicateIdsUpToOwnedCount()
    {
        LobbyRuntimeData lobby = new()
        {
            StoredCompoundIds = new List<string> { "Compound_01", "Compound_01" }
        };
        PartyConsumableRuntime.NormalizeLobby(lobby);

        Assert.That(PartyConsumableRuntime.TryEquip(lobby, 0, "Compound_01"), Is.True);
        Assert.That(PartyConsumableRuntime.TryEquip(lobby, 1, "Compound_01"), Is.True);
        Assert.That(PartyConsumableRuntime.TryEquip(lobby, 2, "Compound_01"), Is.False);
        Assert.That(lobby.EquippedConsumableIds[0], Is.EqualTo("Compound_01"));
        Assert.That(lobby.EquippedConsumableIds[1], Is.EqualTo("Compound_01"));
        Assert.That(lobby.StoredCompoundIds, Is.Empty);
    }

    [Test]
    public void MigrateCharacterCompounds_PreservesEveryDuplicateAndClearsCharacterSlots()
    {
        LobbyRuntimeData lobby = new();
        CharacterRuntimeData first = CharacterWithCompound("Char_01", "Compound_01");
        CharacterRuntimeData second = CharacterWithCompound("Char_02", "Compound_01");
        CharacterRuntimeData third = CharacterWithCompound("Char_03", "Compound_02");

        PartyConsumableRuntime.MigrateCharacterCompounds(
            lobby,
            new[] { first, second, third });

        Assert.That(lobby.EquippedConsumableIds,
            Is.EqualTo(new[] { "Compound_01", "Compound_01", "Compound_02", string.Empty }));
        Assert.That(first.EquippedRelicIds[0], Is.Null.Or.Empty);
        Assert.That(second.EquippedRelicIds[0], Is.Null.Or.Empty);
        Assert.That(third.EquippedRelicIds[0], Is.Null.Or.Empty);

        PartyConsumableRuntime.MigrateCharacterCompounds(
            lobby,
            new[] { first, second, third });
        Assert.That(lobby.EquippedConsumableIds,
            Is.EqualTo(new[] { "Compound_01", "Compound_01", "Compound_02", string.Empty }));
    }

    [Test]
    public void MigrateCharacterCompounds_DoesNotDuplicateSameSavedLoadoutItem()
    {
        LobbyRuntimeData lobby = new()
        {
            CharacterLoadouts = new List<LobbyCharacterLoadoutData>
            {
                new()
                {
                    CharacterId = "Char_01",
                    EquippedRelicIds = new[] { "Compound_01" }
                }
            }
        };
        CharacterRuntimeData character = CharacterWithCompound("Char_01", "Compound_01");

        PartyConsumableRuntime.MigrateCharacterCompounds(lobby, new[] { character });

        Assert.That(lobby.EquippedConsumableIds,
            Is.EqualTo(new[] { "Compound_01", string.Empty, string.Empty, string.Empty }));
        Assert.That(character.EquippedRelicIds[0], Is.Null.Or.Empty);
    }

    [Test]
    public void CopyToBattle_ResetsAllFourUsageFlags()
    {
        LobbyRuntimeData lobby = new()
        {
            EquippedConsumableIds = new[]
            {
                "Compound_01", "Compound_01", "Compound_02", null
            }
        };
        BattleRuntimeData battle = new();

        PartyConsumableRuntime.CopyToBattle(lobby, battle);

        Assert.That(battle.ConsumableSlots.Count, Is.EqualTo(4));
        Assert.That(battle.ConsumableSlots[0].ItemId, Is.EqualTo("Compound_01"));
        Assert.That(battle.ConsumableSlots[1].ItemId, Is.EqualTo("Compound_01"));
        Assert.That(battle.ConsumableSlots[0].IsUsed, Is.False);
        Assert.That(battle.ConsumableSlots[3].ItemId, Is.Empty);
    }

    [Test]
    public void ReturnUnusedToLobby_ReturnsOnlyUnusedSlotsOnce()
    {
        LobbyRuntimeData lobby = new();
        BattleRuntimeData battle = new()
        {
            ConsumableSlots = new List<BattleConsumableSlotRuntimeData>
            {
                new() { ItemId = "Compound_01", IsUsed = true },
                new() { ItemId = "Compound_01", IsUsed = false },
                new() { ItemId = "Compound_02", IsUsed = false },
                new()
            }
        };

        PartyConsumableRuntime.ReturnUnusedToLobby(battle, lobby);
        PartyConsumableRuntime.ReturnUnusedToLobby(battle, lobby);

        Assert.That(lobby.StoredCompoundIds,
            Is.EqualTo(new[] { "Compound_01", "Compound_02" }));
        Assert.That(battle.ConsumablesSettled, Is.True);
    }

    private static CharacterRuntimeData CharacterWithCompound(
        string characterId,
        string compoundId)
    {
        CharacterRuntimeData runtime = new()
        {
            CharacterId = characterId,
            EquippedRelicIds = new string[7]
        };
        runtime.EquippedRelicIds[0] = compoundId;
        return runtime;
    }
}

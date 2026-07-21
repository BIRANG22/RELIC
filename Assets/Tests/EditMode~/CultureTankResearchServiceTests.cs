using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;

public sealed class CultureTankResearchServiceTests
{
    [Test]
    public void TryStartResearch_RemovesSelectedItemFromLobbyBagAndStoresTankState()
    {
        LobbyRuntimeData lobby = new()
        {
            BagItemIds = new List<string> { "Item_001", "Item_002", "Item_001" }
        };

        bool started = CultureTankResearchService.TryStartResearch(
            lobby,
            "CultureTank1",
            "Item_001",
            1000L,
            out string error);

        Assert.That(started, Is.True, error);
        Assert.That(lobby.BagItemIds, Is.EqualTo(new[] { "Item_002", "Item_001" }));
        Assert.That(CultureTankResearchService.TryGetTank(lobby, "CultureTank1", out CultureTankResearchRuntimeData tank), Is.True);
        Assert.That(tank.ItemId, Is.EqualTo("Item_001"));
        Assert.That(tank.StartedAtUtcTicks, Is.EqualTo(1000L));
        Assert.That(tank.DurationSeconds, Is.EqualTo(CultureTankResearchService.DefaultResearchDurationSeconds));
        Assert.That(tank.IsCompleted, Is.False);
    }

    [Test]
    public void TryStartResearch_BlocksBusyTankWithoutRemovingAnotherItem()
    {
        LobbyRuntimeData lobby = new()
        {
            BagItemIds = new List<string> { "Item_002" },
            CultureTankResearches = new List<CultureTankResearchRuntimeData>
            {
                new()
                {
                    TankId = "CultureTank1",
                    ItemId = "Item_001",
                    StartedAtUtcTicks = 1000L,
                    DurationSeconds = 150,
                    IsCompleted = false
                }
            }
        };

        bool started = CultureTankResearchService.TryStartResearch(
            lobby,
            "CultureTank1",
            "Item_002",
            2000L,
            out _);

        Assert.That(started, Is.False);
        Assert.That(lobby.BagItemIds, Is.EqualTo(new[] { "Item_002" }));
    }

    [Test]
    public void TryClaimCompletedResearch_AddsPendingBattleStartEffectAndClearsTank()
    {
        LobbyRuntimeData lobby = new()
        {
            CultureTankResearches = new List<CultureTankResearchRuntimeData>
            {
                new()
                {
                    TankId = "CultureTank1",
                    ItemId = "Item_003",
                    StartedAtUtcTicks = 0L,
                    DurationSeconds = 150,
                    IsCompleted = false
                }
            }
        };
        ItemData item = new()
        {
            ItemId = "Item_003",
            EffectId = "E_Move_First_Attack_Power",
            ValueRate = "2",
            CountRate = "3"
        };

        long completedAt = System.TimeSpan.FromSeconds(150).Ticks;
        bool claimed = CultureTankResearchService.TryClaimCompletedResearch(
            lobby,
            item,
            "CultureTank1",
            completedAt,
            out CultureTankBattleStartEffectRuntimeData effect,
            out string error);

        Assert.That(claimed, Is.True, error);
        Assert.That(effect.SourceItemId, Is.EqualTo("Item_003"));
        Assert.That(effect.EffectId, Is.EqualTo("E_Move_First_Attack_Power"));
        Assert.That(effect.Value, Is.EqualTo(2));
        Assert.That(effect.Count, Is.EqualTo(3));
        Assert.That(effect.RemainingBattleStarts, Is.EqualTo(3));
        Assert.That(lobby.PendingCultureTankBattleStartEffects, Has.Count.EqualTo(1));
        Assert.That(CultureTankResearchService.TryGetTank(lobby, "CultureTank1", out _), Is.False);
    }

    [Test]
    public void Transfer_CopiesPendingCultureTankBattleStartEffectsWithoutCarryingLobbyBagItems()
    {
        LobbyRuntimeData lobby = new()
        {
            BagItemIds = new List<string> { "Item_001" },
            PendingCultureTankBattleStartEffects = new List<CultureTankBattleStartEffectRuntimeData>
            {
                new()
                {
                    SourceItemId = "Item_003",
                    EffectId = "E_Move_First_Attack_Power",
                    Value = 1,
                    Count = 1,
                    RemainingBattleStarts = 3
                }
            }
        };
        BattleRuntimeData battle = new();

        LobbyBattleRuntimeTransferResult result =
            new LobbyBattleRuntimeTransferService().Transfer(lobby, battle, null);

        Assert.That(result.Succeeded, Is.True, result.Error);
        Assert.That(battle.BagItemIds, Is.Empty);
        Assert.That(battle.CultureTankBattleStartEffects, Has.Count.EqualTo(1));
        Assert.That(battle.CultureTankBattleStartEffects[0].EffectId, Is.EqualTo("E_Move_First_Attack_Power"));
        Assert.That(lobby.PendingCultureTankBattleStartEffects, Is.Empty);
    }

    [Test]
    public void BattleStartEffectService_AppliesStatusToEveryCharacterAndConsumesOneBattleStart()
    {
        BattleRuntimeData battle = new()
        {
            CultureTankBattleStartEffects = new List<CultureTankBattleStartEffectRuntimeData>
            {
                new()
                {
                    SourceItemId = "Item_003",
                    EffectId = "E_Move_First_Attack_Power",
                    Value = 2,
                    Count = 3,
                    RemainingBattleStarts = 3
                }
            }
        };
        CharacterRuntimeData first = new() { CharacterId = "Char_01" };
        CharacterRuntimeData second = new() { CharacterId = "Char_02" };

        bool applied = CultureTankBattleStartEffectService.ApplyToPartyAndConsume(
            battle,
            new[] { first, second });

        Assert.That(applied, Is.True);
        Assert.That(first.StatusEffects[0].EffectId, Is.EqualTo("E_Move_First_Attack_Power"));
        Assert.That(first.StatusEffects[0].Stack, Is.EqualTo(6));
        Assert.That(second.StatusEffects[0].Stack, Is.EqualTo(6));
        Assert.That(battle.CultureTankBattleStartEffects[0].RemainingBattleStarts, Is.EqualTo(2));
    }

    [Test]
    public void BattleStartEffectService_AppliesArmorAsShieldInsteadOfStatus()
    {
        BattleRuntimeData battle = new()
        {
            CultureTankBattleStartEffects = new List<CultureTankBattleStartEffectRuntimeData>
            {
                new()
                {
                    SourceItemId = "Item_002",
                    EffectId = "E_Armor",
                    Value = 5,
                    Count = 2,
                    RemainingBattleStarts = 1
                }
            }
        };
        CharacterRuntimeData character = new() { CharacterId = "Char_01" };

        CultureTankBattleStartEffectService.ApplyToPartyAndConsume(
            battle,
            new[] { character });

        Assert.That(character.CurrentShield, Is.EqualTo(10));
        Assert.That(character.StatusEffects, Is.Empty);
        Assert.That(battle.CultureTankBattleStartEffects, Is.Empty);
    }
}

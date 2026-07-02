using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;

public class BattleGridEffectServiceTests
{
    [TearDown]
    public void TearDown()
    {
        BattleRandom.ClearSeed();
    }

    [Test]
    public void SpawnRandomEffects_CreatesTwoOrThreeAndExcludesOccupiedGrids()
    {
        GridEffectDatabase database = CreateDatabase(
            new GridEffectData { GridEffectID = "GR_debris", Passed = 0 },
            new GridEffectData { GridEffectID = "GR_thorn", Passed = 1, ValueRate = 5, EffectIds = "E_Damage" }
        );
        BattleGridEffectService service = new(database);
        BattleGridEffectState state = new();
        HashSet<int> occupied = new() { 0, 5, 6 };

        BattleRandom.SetSeed(17);
        IReadOnlyList<BattleGridEffectPlacement> placements =
            service.SpawnRandomEffects(state, 4, 3, occupied);

        Assert.That(placements.Count, Is.InRange(2, 3));

        foreach (BattleGridEffectPlacement placement in placements)
        {
            Assert.That(occupied.Contains(placement.GridIndex), Is.False);
            Assert.That(database.TryGet(placement.GridEffectId, out _), Is.True);
            Assert.That(state.TryGetEffectId(placement.GridIndex, out string placedId), Is.True);
            Assert.That(placedId, Is.EqualTo(placement.GridEffectId));
        }
    }

    [Test]
    public void IsBlocked_ReturnsTrueOnlyForPassedZeroEffects()
    {
        GridEffectDatabase database = CreateDatabase(
            new GridEffectData { GridEffectID = "GR_debris", Passed = 0 },
            new GridEffectData { GridEffectID = "GR_helmet", Passed = 1, ValueRate = 4, EffectIds = "E_Armor" }
        );
        BattleGridEffectService service = new(database);
        BattleGridEffectState state = new();
        state.Place(3, "GR_debris");
        state.Place(4, "GR_helmet");

        Assert.That(service.IsBlocked(state, 3), Is.True);
        Assert.That(service.IsBlocked(state, 4), Is.False);
        Assert.That(service.IsBlocked(state, 5), Is.False);
    }

    [Test]
    public void ApplyToPlayer_ChangesRuntimeStateAndConsumesConsumableEffects()
    {
        GridEffectDatabase database = CreateDatabase(
            new GridEffectData { GridEffectID = "GR_thorn", Passed = 1, Consumable = 1, ValueRate = 5, EffectIds = "E_Damage" },
            new GridEffectData { GridEffectID = "GR_helmet", Passed = 1, Consumable = 0, ValueRate = 4, EffectIds = "E_Armor" },
            new GridEffectData { GridEffectID = "GR_bandage", Passed = 1, Consumable = 1, ValueRate = 6, EffectIds = "E_Recover" }
        );
        BattleGridEffectService service = new(database);
        BattleGridEffectState state = new();
        CharacterRuntimeData runtime = new()
        {
            CharacterId = "C_Player",
            MaxHP = 20,
            CurrentHP = 12,
            CurrentShield = 2
        };

        state.Place(1, "GR_thorn");
        BattleGridEffectApplyResult thornResult = service.ApplyToPlayer(state, 1, runtime);

        Assert.That(thornResult.Applied, Is.True);
        Assert.That(thornResult.Consumed, Is.True);
        Assert.That(runtime.CurrentShield, Is.EqualTo(0));
        Assert.That(runtime.CurrentHP, Is.EqualTo(9));
        Assert.That(state.TryGetEffectId(1, out _), Is.False);

        state.Place(2, "GR_helmet");
        BattleGridEffectApplyResult helmetResult = service.ApplyToPlayer(state, 2, runtime);

        Assert.That(helmetResult.Applied, Is.True);
        Assert.That(helmetResult.Consumed, Is.False);
        Assert.That(runtime.CurrentShield, Is.EqualTo(4));
        Assert.That(state.TryGetEffectId(2, out _), Is.True);

        state.Place(3, "GR_bandage");
        service.ApplyToPlayer(state, 3, runtime);

        Assert.That(runtime.CurrentHP, Is.EqualTo(15));
        Assert.That(state.TryGetEffectId(3, out _), Is.False);
    }

    [Test]
    public void ApplyToMonster_UsesSameDamageArmorHealAndStatusRules()
    {
        GridEffectDatabase database = CreateDatabase(
            new GridEffectData { GridEffectID = "GR_thorn", Passed = 1, Consumable = 1, ValueRate = 5, EffectIds = "E_Damage" },
            new GridEffectData { GridEffectID = "GR_helmet", Passed = 1, Consumable = 0, ValueRate = 4, EffectIds = "E_Armor" },
            new GridEffectData { GridEffectID = "GR_bandage", Passed = 1, Consumable = 1, ValueRate = 6, EffectIds = "E_Recover" },
            new GridEffectData { GridEffectID = "GR_poison", Passed = 1, Consumable = 1, ValueRate = 2, EffectIds = "E_Addicted" }
        );
        BattleGridEffectService service = new(database);
        BattleGridEffectState state = new();
        MonsterRuntimeData runtime = new("M_Runtime", new MonsterMasterData { MonsterId = "M_Test", HP = 20 })
        {
            CurrentHP = 12,
            CurrentShield = 2
        };

        state.Place(1, "GR_thorn");
        service.ApplyToMonster(state, 1, runtime);
        Assert.That(runtime.CurrentShield, Is.EqualTo(0));
        Assert.That(runtime.CurrentHP, Is.EqualTo(9));

        state.Place(2, "GR_helmet");
        service.ApplyToMonster(state, 2, runtime);
        Assert.That(runtime.CurrentShield, Is.EqualTo(4));

        state.Place(3, "GR_bandage");
        service.ApplyToMonster(state, 3, runtime);
        Assert.That(runtime.CurrentHP, Is.EqualTo(15));

        state.Place(4, "GR_poison");
        BattleGridEffectApplyResult poisonResult = service.ApplyToMonster(state, 4, runtime);

        Assert.That(poisonResult.AppliedEffectIds, Is.EquivalentTo(new[] { "E_Addicted" }));
        Assert.That(runtime.StatusEffects, Has.Count.EqualTo(1));
        Assert.That(runtime.StatusEffects[0].EffectId, Is.EqualTo("E_Addicted"));
        Assert.That(runtime.StatusEffects[0].Stack, Is.EqualTo(2));
        Assert.That(state.TryGetEffectId(4, out _), Is.False);
    }

    private static GridEffectDatabase CreateDatabase(params GridEffectData[] effects)
    {
        GridEffectDatabase database = new();
        database.Initialize(effects);
        return database;
    }
}

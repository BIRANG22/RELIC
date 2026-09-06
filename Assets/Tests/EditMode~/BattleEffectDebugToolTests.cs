using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;

public sealed class BattleEffectDebugToolTests
{
    [Test]
    public void GetDefaultPresets_NoLongerProvidesHardcodedLoadoutCatalog()
    {
        IReadOnlyList<BattleEffectDebugPreset> presets = BattleEffectDebugTool.GetDefaultPresets();

        Assert.That(presets, Is.Empty);
    }

    [Test]
    public void ApplyPreset_EquipsOnlyPassiveRelicsAndLeavesCompoundSlotEmpty()
    {
        CharacterRuntimeData runtime = CreateRuntime();
        BattleEffectDebugPreset preset = new(
            "Mixed",
            "Mixed",
            new[] { "Compound_01", "Relic_P_01", "Relic_P_06" },
            null);

        BattleEffectDebugTool.ApplyPreset(runtime, preset);

        Assert.That(runtime.EquippedRelicIds[0], Is.Null.Or.Empty);
        Assert.That(runtime.EquippedRelicIds[1], Is.EqualTo("Relic_P_01"));
        Assert.That(runtime.EquippedRelicIds[2], Is.EqualTo("Relic_P_06"));
    }

    [Test]
    public void SetHpPercent_ClampsToAliveHpWithinMaxHp()
    {
        CharacterRuntimeData runtime = CreateRuntime();

        BattleEffectDebugTool.SetHpPercent(runtime, 0.3f);
        Assert.That(runtime.CurrentHP, Is.EqualTo(30));

        BattleEffectDebugTool.SetHpPercent(runtime, 0f);
        Assert.That(runtime.CurrentHP, Is.EqualTo(1));

        BattleEffectDebugTool.SetHpPercent(runtime, 2f);
        Assert.That(runtime.CurrentHP, Is.EqualTo(100));
    }

    [Test]
    public void SetResourceValues_ClampToRuntimeLimits()
    {
        CharacterRuntimeData runtime = CreateRuntime();

        BattleEffectDebugTool.SetCurrentCost(runtime, 99);
        BattleEffectDebugTool.SetCurrentResource(runtime, 99, 5);

        Assert.That(runtime.CurrentCost, Is.EqualTo(3));
        Assert.That(runtime.CurrentResource, Is.EqualTo(5));
    }

    [Test]
    public void SetFullResources_FillsCostAndKeepsResolvedResourceValue()
    {
        CharacterRuntimeData runtime = CreateRuntime();
        runtime.CurrentCost = 0;
        runtime.CurrentResource = 4;

        BattleEffectDebugTool.SetFullResources(runtime);

        Assert.That(runtime.CurrentCost, Is.EqualTo(3));
        Assert.That(runtime.CurrentResource, Is.EqualTo(4));
    }

    [Test]
    public void AddOrStackStatus_AddsNewStatusAndStacksExistingOne()
    {
        CharacterRuntimeData runtime = CreateRuntime();

        BattleEffectDebugTool.AddOrStackStatus(runtime.StatusEffects, "E_Poison", 2, 1);
        BattleEffectDebugTool.AddOrStackStatus(runtime.StatusEffects, "E_Poison", 3, 1);

        Assert.That(runtime.StatusEffects, Has.Count.EqualTo(1));
        Assert.That(runtime.StatusEffects[0].Stack, Is.EqualTo(5));
    }

    private static CharacterRuntimeData CreateRuntime()
    {
        return new CharacterRuntimeData
        {
            CharacterId = "Char_Test",
            MaxHP = 100,
            CurrentHP = 100,
            MaxCost = 3,
            CurrentCost = 3,
            CurrentResource = 0,
            EquippedRelicIds = new string[7],
            EquippedRuneIds = new string[12],
            StatusEffects = new List<StatusEffectRuntimeData>()
        };
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;

public sealed class BattleErosionEffectServiceTests
{
    [Test]
    public void ResolveValue_ExclusiveGroupUsesHighestTier()
    {
        ErosionDatabase database = CreateDatabase(
            Entry("low", 1, 10, "group", "Exclusive", BattleErosionEffectService.MonsterMaxHPPercent),
            Entry("high", 3, 30, "group", "Exclusive", BattleErosionEffectService.MonsterMaxHPPercent));

        Assert.That(BattleErosionEffectService.ResolveValue(
            new[] { "low", "high" }, database, BattleErosionEffectService.MonsterMaxHPPercent), Is.EqualTo(30));
    }

    [Test]
    public void ResolveValue_IndependentEntriesAreAdded()
    {
        ErosionDatabase database = CreateDatabase(
            Entry("a", 1, 1, "a", "Independent", BattleErosionEffectService.MoveFinalManaCostAdd),
            Entry("b", 2, 2, "b", "Independent", BattleErosionEffectService.MoveFinalManaCostAdd));

        Assert.That(BattleErosionEffectService.ResolveValue(
            new[] { "a", "b" }, database, BattleErosionEffectService.MoveFinalManaCostAdd), Is.EqualTo(3));
    }

    [TestCase(10, 25, 13)]
    [TestCase(9, -20, 7)]
    [TestCase(1, -100, 1)]
    public void ApplyPercent_RoundsAndHonorsMinimum(int value, int percent, int expected)
    {
        Assert.That(BattleErosionEffectService.ApplyPercent(value, percent, 1), Is.EqualTo(expected));
    }

    [Test]
    public void IsRelicSlotLocked_LocksOnlyVisibleLastSlotWhenEffectIsEnabled()
    {
        Assert.That(BattleErosionEffectService.IsRelicSlotLocked(
            BattleErosionEffectService.LastRelicSlotIndex, true), Is.True);
        Assert.That(BattleErosionEffectService.IsRelicSlotLocked(
            BattleErosionEffectService.LastRelicSlotIndex - 1, true), Is.False);
        Assert.That(BattleErosionEffectService.IsRelicSlotLocked(
            BattleErosionEffectService.LastRelicSlotIndex, false), Is.False);
    }

    [Test]
    public void Transfer_CopiesSelectedIdsWithoutSharingList()
    {
        LobbyRuntimeData lobby = new() { SelectedErosionDifficultyIds = new List<string> { " Erosion_01_01 ", "Erosion_01_01" } };
        BattleRuntimeData battle = new();

        LobbyBattleRuntimeTransferResult result = new LobbyBattleRuntimeTransferService().Transfer(lobby, battle, null);

        Assert.That(result.Succeeded, Is.True);
        Assert.That(battle.SelectedErosionDifficultyIds, Is.EqualTo(new[] { "Erosion_01_01" }));
        lobby.SelectedErosionDifficultyIds.Clear();
        Assert.That(battle.SelectedErosionDifficultyIds, Has.Count.EqualTo(1));
    }

    private static ErosionDatabase CreateDatabase(params ErosionData[] entries)
    {
        ErosionDatabase database = new();
        database.Initialize(entries);
        return database;
    }

    private static ErosionData Entry(string id, int tier, int value, string group, string mode, string type)
    {
        return new ErosionData
        {
            DifficultyId = id,
            Tier = tier,
            EffectValue = value,
            GroupId = group,
            SelectionMode = mode,
            EffectType = type,
            Selectable = true
        };
    }
}

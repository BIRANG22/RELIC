using NUnit.Framework;

public sealed class BattleRewardRelicEquipPolicyTests
{
    [Test]
    public void FindFirstEmptySlot_ReturnsActiveSlotOnly_ForActiveRelic()
    {
        string[] slots = { "", "Relic_01", "", "", "", "", "" };

        Assert.That(BattleRewardRelicEquipPolicy.FindFirstEmptySlot(slots, true), Is.Zero);

        slots[0] = "Compound_01";
        Assert.That(BattleRewardRelicEquipPolicy.FindFirstEmptySlot(slots, true), Is.EqualTo(-1));
    }

    [Test]
    public void FindFirstEmptySlot_ReturnsFirstPassiveEmptySlot_WithoutReplacingOccupiedSlots()
    {
        string[] slots = { "Compound_01", "Relic_01", "Relic_02", "", "Relic_04", "", "" };

        Assert.That(BattleRewardRelicEquipPolicy.FindFirstEmptySlot(slots, false), Is.EqualTo(3));
    }

    [Test]
    public void FindFirstEmptySlot_ReturnsMinusOne_WhenCompatibleSlotsAreFull()
    {
        string[] slots =
        {
            "Compound_01", "Relic_01", "Relic_02", "Relic_03",
            "Relic_04", "Relic_05", "Relic_06"
        };

        Assert.That(BattleRewardRelicEquipPolicy.FindFirstEmptySlot(slots, false), Is.EqualTo(-1));
    }

    [Test]
    public void FindFirstEmptySlot_ReturnsMinusOne_ForMissingSlots()
    {
        Assert.That(BattleRewardRelicEquipPolicy.FindFirstEmptySlot(null, false), Is.EqualTo(-1));
    }
}

using NUnit.Framework;
using Relic.Gameplay.Data;

public sealed class BattleRuntimeMaxResourceDisplayTests
{
    [Test]
    public void ResolvePartyMaxCost_DoesNotAddRunBonusTwice()
    {
        CharacterRuntimeData runtime = new()
        {
            MaxCost = 12,
            RunMaxCostBonus = 2
        };

        Assert.That(BattleCharacterPanelUI.ResolvePartyMaxCost(runtime), Is.EqualTo(12));
    }

    [Test]
    public void ResolvePartyMaxHp_DoesNotAddRunBonusTwice()
    {
        CharacterRuntimeData runtime = new()
        {
            MaxHP = 24,
            RunMaxHPBonus = 4
        };

        Assert.That(BattleCharacterPanelUI.ResolvePartyMaxHP(runtime), Is.EqualTo(24));
    }

    [Test]
    public void ResolvePartyMaxValues_NullRuntime_ReturnsZero()
    {
        Assert.That(BattleCharacterPanelUI.ResolvePartyMaxCost(null), Is.Zero);
        Assert.That(BattleCharacterPanelUI.ResolvePartyMaxHP(null), Is.Zero);
    }
}

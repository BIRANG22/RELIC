using NUnit.Framework;
using Relic.Gameplay.Battle;

public class MonsterTargetingAndGridOwnershipTests
{
    [TestCase(BattleDirection.Left, true, true, BattleDirection.Left)]
    [TestCase(BattleDirection.Left, true, false, BattleDirection.Left)]
    [TestCase(BattleDirection.Left, false, true, BattleDirection.Right)]
    [TestCase(BattleDirection.Right, false, false, BattleDirection.Right)]
    public void VespaExecutionDirection_PrefersReservedUnlessOnlyOppositeHasTarget(
        BattleDirection reservedDirection,
        bool reservedHasTarget,
        bool oppositeHasTarget,
        BattleDirection expected)
    {
        Assert.That(VespaExecutionDirectionPolicy.Resolve(
            reservedDirection,
            reservedHasTarget,
            oppositeHasTarget), Is.EqualTo(expected));
    }

    [Test]
    public void GridEffectOwner_IsPreservedUntilEffectIsRemoved()
    {
        BattleGridEffectState state = new();

        Assert.That(state.Place(7, "GR_spider_egg", 2, 10, "runtime-arabella", "Mon_12_1"), Is.True);
        Assert.That(state.TryGetOwner(7, out string runtimeId, out string monsterId), Is.True);
        Assert.That(runtimeId, Is.EqualTo("runtime-arabella"));
        Assert.That(monsterId, Is.EqualTo("Mon_12_1"));

        Assert.That(state.Remove(7), Is.True);
        Assert.That(state.TryGetOwner(7, out _, out _), Is.False);
    }

    [Test]
    public void GridEffectOwner_IsIncludedInExpiredPlacement()
    {
        BattleGridEffectState state = new();
        state.Place(3, "GR_spider_egg", 1, 10, "runtime-arabella", "Mon_12_1");

        BattleGridEffectPlacement expired = state.AdvanceDurationsDetailed()[0];

        Assert.That(expired.OwnerRuntimeId, Is.EqualTo("runtime-arabella"));
        Assert.That(expired.OwnerMonsterId, Is.EqualTo("Mon_12_1"));
    }

    [TestCase("runtime-arabella", "runtime-arabella", false)]
    [TestCase("runtime-arabella", "runtime-other", true)]
    [TestCase("", "runtime-arabella", true)]
    public void GridEffectDamagePolicy_RejectsOnlyTheOwningRuntime(
        string ownerRuntimeId,
        string attackerRuntimeId,
        bool expected)
    {
        Assert.That(BattleGridEffectDamagePolicy.CanMonsterDamage(
            ownerRuntimeId,
            attackerRuntimeId), Is.EqualTo(expected));
    }
}

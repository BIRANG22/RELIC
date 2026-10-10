using NUnit.Framework;

public class RuneEffectRulesTests
{
    [TestCase(100f, false, 150f)]
    [TestCase(100f, true, 175f)]
    public void Vulnerable_UsesGlobalOrRuneEnhancedMultiplier(float damage, bool enhanced, float expected)
    {
        Assert.That(RuneEffectRules.ApplyVulnerable(damage, enhanced), Is.EqualTo(expected));
    }

    [Test]
    public void Weaken_ReducesOutgoingDamageByThirtyPercent()
    {
        Assert.That(RuneEffectRules.ApplyWeaken(100f), Is.EqualTo(70f));
    }

    [TestCase(50, 100, true)]
    [TestCase(51, 100, false)]
    [TestCase(0, 0, false)]
    public void LowHpBonus_UsesInclusiveHalfHpBoundary(int hp, int maxHp, bool expected)
    {
        Assert.That(RuneEffectRules.IsAtOrBelowHalfHp(hp, maxHp), Is.EqualTo(expected));
    }

    [TestCase(2, 1, 1)]
    [TestCase(2, 2, 0)]
    [TestCase(3, 1, 1)]
    [TestCase(4, 2, 1)]
    public void ConditionalDamageDelta_RequiresThreeHitsOrSingleTarget(int hits, int targets, int expected)
    {
        Assert.That(RuneEffectRules.ResolveConditionalDamageDelta(hits, targets), Is.EqualTo(expected));
    }

    [TestCase(0, true, true, 1)]
    [TestCase(0, false, true, 0)]
    [TestCase(1, true, true, 0)]
    [TestCase(0, true, false, 0)]
    public void FirstRegisteredAttack_AddsOneHitOnlyForFirstSkill(
        int slotIndex,
        bool isFirstSkillInSlot,
        bool attack,
        int expected)
    {
        Assert.That(
            RuneEffectRules.ResolveFirstRegisteredAttackCountDelta(
                slotIndex,
                isFirstSkillInSlot,
                attack),
            Is.EqualTo(expected));
    }
}

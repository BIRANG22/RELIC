using NUnit.Framework;
using Relic.Gameplay.Data;

public class RuneEffectRulesTests
{
    [TestCase("E_Damage", SkillType.Attack, 1, 1)]
    [TestCase("E_Cure", SkillType.Buff, 1, 1)]
    [TestCase("E_Armor", SkillType.Buff, 2, 2)]
    [TestCase("E_Poison", SkillType.Debuff, 1, 1)]
    [TestCase("E_Bleed", SkillType.Debuff, 1, 1)]
    [TestCase("E_Move", SkillType.Attack, 1, 0)]
    public void GeneralRuneValueDelta_MatchesEffectKind(
        string effectId,
        SkillType skillType,
        int configuredValue,
        int expected)
    {
        Assert.That(
            RuneEffectRules.ResolveGeneralRuneValueDelta(effectId, skillType, configuredValue),
            Is.EqualTo(expected));
    }

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
    public void ConditionalDamageDelta_RequiresThreeHitsOrSingleLivingEnemy(int hits, int livingEnemies, int expected)
    {
        Assert.That(RuneEffectRules.ResolveConditionalDamageDelta(hits, livingEnemies), Is.EqualTo(expected));
    }

    [TestCase(0, true, 1)]
    [TestCase(1, true, 0)]
    [TestCase(0, false, 0)]
    public void FirstRegisteredAttack_AddsOneHitOnlyWhenNoEarlierAttack(
        int earlierAttackReservations,
        bool attack,
        int expected)
    {
        Assert.That(
            RuneEffectRules.ResolveFirstRegisteredAttackCountDelta(
                earlierAttackReservations,
                attack),
            Is.EqualTo(expected));
    }

    [TestCase(0, false, true)]
    [TestCase(1, false, false)]
    [TestCase(0, true, false)]
    public void Liberation_AppliesOnlyToFirstNonMoveMemory(
        int earlierSkillReservations,
        bool isMove,
        bool expected)
    {
        Assert.That(
            RuneEffectRules.IsFirstNonMoveMemory(earlierSkillReservations, isMove),
            Is.EqualTo(expected));
    }

    [TestCase(10, 10, true)]
    [TestCase(9, 10, false)]
    public void Momentum_ChecksArmorThresholdAtTurnEnd(int armor, int threshold, bool expected)
    {
        Assert.That(RuneEffectRules.MeetsArmorThresholdAtTurnEnd(armor, threshold), Is.EqualTo(expected));
    }

    [Test]
    public void Guardian_MapsToBarrierInsteadOfWard()
    {
        Assert.That(RuneEffectRules.ResolveTurnStartRuneStatus("E_Rune_Turn1_Ward"), Is.EqualTo("E_Barrier"));
    }

    [Test]
    public void FightingSpirit_LowHpBonusCanBeSharedByTooltipAndDamage()
    {
        Assert.That(RuneEffectRules.ApplyLowHpDamagePercent(10f, 30, true), Is.EqualTo(13f));
        Assert.That(RuneEffectRules.ApplyLowHpDamagePercent(10f, 30, false), Is.EqualTo(10f));
    }
}

using NUnit.Framework;

public class NewSkillEffectRulesTests
{
    [TestCase(12, 2, 24)]
    [TestCase(-3, 4, 0)]
    public void ArmorStrike_UsesCurrentArmor(int armor, int rate, int expected)
    {
        Assert.That(NewSkillEffectRules.ResolveArmorStrikeDamage(armor, rate), Is.EqualTo(expected));
    }

    [TestCase(10, true, 15)]
    [TestCase(10, false, 10)]
    public void BleedingDamageBonus_AddsFiftyPercent(int damage, bool bleeding, int expected)
    {
        Assert.That(NewSkillEffectRules.ApplyBleedingDamageBonus(damage, bleeding), Is.EqualTo(expected));
    }

    [TestCase(10, 3, 2, 16)]
    [TestCase(10, 0, 2, 10)]
    public void VulnerableStrike_AddsStackTimesRate(int value, int vulnerable, int rate, int expected)
    {
        Assert.That(NewSkillEffectRules.ResolveVulnerableStrikeDamage(value, vulnerable, rate), Is.EqualTo(expected));
    }

    [TestCase(7, 3, 2)]
    [TestCase(2, 3, 0)]
    [TestCase(7, 0, 0)]
    public void PoisonByTargetPoison_FloorsDivision(int poison, int divisor, int expected)
    {
        Assert.That(
            NewSkillEffectRules.ResolvePoisonByTargetPoison(poison, divisor),
            Is.EqualTo(expected));
    }

    [TestCase("E_StrikeByVulnerable", 3, 1)]
    [TestCase("E_DamageUpIfBleeding", 3, 3)]
    [TestCase("E_StrikeCountByBuff", 3, 3)]
    public void ResolveDirectDamageHitCount_PreservesEffectSpecificCountMeaning(
        string effectId,
        int configuredCount,
        int expected)
    {
        Assert.That(
            NewSkillEffectRules.ResolveDirectDamageHitCount(effectId, configuredCount),
            Is.EqualTo(expected));
    }

    [TestCase("E_StrikeByVulnerable", 3, 3)]
    [TestCase("E_Strike", 3, 1)]
    public void ResolveDirectDamageParameterCount_UsesVulnerableRateOnly(
        string effectId,
        int configuredCount,
        int expected)
    {
        Assert.That(
            NewSkillEffectRules.ResolveDirectDamageParameterCount(effectId, configuredCount),
            Is.EqualTo(expected));
    }

    [TestCase("E_Rush", false)]
    [TestCase("E_Move", true)]
    public void ShouldApplyMoveCollisionEffect_RushOnlyRecordsCollision(
        string effectId,
        bool expected)
    {
        Assert.That(
            NewSkillEffectRules.ShouldApplyMoveCollisionEffect(effectId),
            Is.EqualTo(expected));
    }

    [TestCase(4, 3, 12)]
    [TestCase(0, 3, 0)]
    public void RestoreManaPerHit_UsesSuccessfulHitCount(int hits, int rate, int expected)
    {
        Assert.That(NewSkillEffectRules.ResolveManaRestore(hits, rate), Is.EqualTo(expected));
    }

    [TestCase(3, 2, 6)]
    [TestCase(0, 2, 0)]
    public void ValueUpOnKill_UsesKillCount(int kills, int rate, int expected)
    {
        Assert.That(NewSkillEffectRules.ResolvePermanentValueBonus(kills, rate), Is.EqualTo(expected));
    }

    [TestCase(2, 3, 5)]
    [TestCase(2, 0, 2)]
    public void StrikeCountByDebuff_AddsDistinctHarmfulTypes(int baseCount, int types, int expected)
    {
        Assert.That(NewSkillEffectRules.ResolveConditionalHitCount(baseCount, types), Is.EqualTo(expected));
    }

    [TestCase(4, 2, 6)]
    [TestCase(0, 2, 2)]
    public void ValueUpOnHit_AddsSuccessfulHitBonus(int hits, int rate, int expected)
    {
        Assert.That(NewSkillEffectRules.ResolveValueUpOnHit(hits, rate), Is.EqualTo(expected));
    }
}

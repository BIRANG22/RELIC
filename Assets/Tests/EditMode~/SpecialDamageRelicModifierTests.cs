using NUnit.Framework;

public class SpecialDamageRelicModifierTests
{
    [Test]
    public void ArmorStrike_ResolvesArmorDamageBeforeRelicModifiers()
    {
        int actual = BattleEquipmentEffectService.ResolvePlayerDamageBaseValue(
            "E_ArmorStrike",
            effectValue: 1,
            currentShield: 4,
            maxHp: 0,
            currentHp: 0);

        Assert.That(actual, Is.EqualTo(4));
    }

    [Test]
    public void RandomStrike_AppliesAttackFlatDamageDelta()
    {
        int actual = BattleEquipmentEffectService.ApplyDamageRelicModifier(
            currentValue: 10,
            effectId: "E_Attack_Value_Delta",
            modifierValue: 2);

        Assert.That(actual, Is.EqualTo(12));
    }

    [Test]
    public void MissingHpStrike_UsesPercentageOfCastersMissingHp()
    {
        int actual = BattleEquipmentEffectService.ResolvePlayerDamageBaseValue(
            "E_MissingHPStrike",
            effectValue: 50,
            currentShield: 0,
            maxHp: 100,
            currentHp: 50);

        Assert.That(actual, Is.EqualTo(25));
    }

    [TestCase(true, 9)]
    [TestCase(false, 8)]
    public void DamageRelicModifiers_AreAppliedInEquippedOrder(
        bool flatBeforePercent,
        int expected)
    {
        int value = 4;
        if (flatBeforePercent)
        {
            value = BattleEquipmentEffectService.ApplyDamageRelicModifier(
                value, "E_Attack_Value_Delta", 2);
            value = BattleEquipmentEffectService.ApplyDamageRelicModifier(
                value, "E_Unique_Skill_Value_Percent", 50);
        }
        else
        {
            value = BattleEquipmentEffectService.ApplyDamageRelicModifier(
                value, "E_Unique_Skill_Value_Percent", 50);
            value = BattleEquipmentEffectService.ApplyDamageRelicModifier(
                value, "E_Attack_Value_Delta", 2);
        }

        Assert.That(value, Is.EqualTo(expected));
    }

    [Test]
    public void Rush_DoesNotApplyDamageRelicModifiers()
    {
        Assert.That(BattleEquipmentEffectService.IsPlayerDamageEffect("E_Rush"), Is.False);
    }

    [TestCase("E_Strike")]
    [TestCase("E_Pierce")]
    [TestCase("E_MissingHPStrike")]
    [TestCase("E_ArmorStrike")]
    [TestCase("E_RandomStrike")]
    [TestCase("E_StrikeByVulnerable")]
    [TestCase("E_StrikeByTargetPoison")]
    [TestCase("E_DamageUpIfBleeding")]
    [TestCase("E_StrikeCountByBuff")]
    [TestCase("E_StrikeCountByDebuff")]
    public void IsPlayerDamageEffect_DirectDamageEffectsReturnTrue(string effectId)
    {
        Assert.That(BattleEquipmentEffectService.IsPlayerDamageEffect(effectId), Is.True);
    }

    [TestCase("E_Rush")]
    [TestCase("E_Move")]
    [TestCase("E_Knockback")]
    public void IsPlayerDamageEffect_NonDamageEffectsReturnFalse(string effectId)
    {
        Assert.That(BattleEquipmentEffectService.IsPlayerDamageEffect(effectId), Is.False);
    }

    [TestCase("E_Strike", true)]
    [TestCase("E_Pierce", true)]
    [TestCase("E_MissingHPStrike", true)]
    [TestCase("E_ArmorStrike", false)]
    [TestCase("E_RandomStrike", false)]
    [TestCase("E_StrikeByVulnerable", false)]
    public void IsPlayerHitCountEffect_PreservesExistingHitCountScope(
        string effectId,
        bool expected)
    {
        Assert.That(BattleEquipmentEffectService.IsPlayerHitCountEffect(effectId), Is.EqualTo(expected));
    }
}

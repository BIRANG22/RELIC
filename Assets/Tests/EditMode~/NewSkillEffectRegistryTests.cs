using NUnit.Framework;

public class NewSkillEffectRegistryTests
{
    [TestCase("E_ArmorStrike")]
    [TestCase("E_MissSelfDamage")]
    [TestCase("E_ValueUpOnKill")]
    [TestCase("E_ValueUpOnHit")]
    [TestCase("E_DamageUpIfBleeding")]
    [TestCase("E_Rush")]
    [TestCase("E_Bondage")]
    [TestCase("E_PoisonByTargetPoison")]
    [TestCase("E_TriggerPoison")]
    [TestCase("E_PoisonTrigger")]
    [TestCase("E_RestoreManaPerHitTarget")]
    [TestCase("E_RandomStrike")]
    [TestCase("E_StrikeByVulnerable")]
    [TestCase("E_StrikeCountByBuff")]
    [TestCase("E_StrikeCountByDebuff")]
    public void Registry_ContainsEveryNewSkillEffectId(string effectId)
    {
        Assert.That(new BattleEffectRegistry().Get(effectId), Is.Not.Null);
    }
}

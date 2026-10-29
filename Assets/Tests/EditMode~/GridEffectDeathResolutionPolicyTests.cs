using NUnit.Framework;
using Relic.Gameplay.Data;

public sealed class GridEffectDeathResolutionPolicyTests
{
    [TestCase("Mon_01", true)]
    [TestCase("Mon_12_1", false)]
    [TestCase("Mon_12_2", true)]
    public void ShouldPlayDeadPresentation_UsesMonsterDeathPolicy(string monsterId, bool expected)
    {
        MonsterRuntimeData runtime = new("test-runtime", null)
        {
            MonsterId = monsterId,
            CurrentHP = 0
        };

        Assert.That(
            BattleGridEffectController.ShouldPlayDeadPresentation(runtime),
            Is.EqualTo(expected));
    }

    [Test]
    public void ShouldPlayDeadPresentation_LivingMonster_ReturnsFalse()
    {
        MonsterRuntimeData runtime = new("test-runtime", null)
        {
            MonsterId = "Mon_01",
            CurrentHP = 1
        };

        Assert.That(BattleGridEffectController.ShouldPlayDeadPresentation(runtime), Is.False);
    }
}

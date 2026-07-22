using NUnit.Framework;
using Relic.Gameplay.Data;

public class DebugBattleTargetRulesTests
{
    [Test]
    public void Configure_SetsDebugRuntimeIdAndHpTo999()
    {
        MonsterRuntimeData data = new("original", null);

        DebugBattleTargetRules.Configure(data);

        Assert.That(data.RuntimeId, Is.EqualTo(DebugBattleTargetRules.RuntimeId));
        Assert.That(data.MaxHP, Is.EqualTo(999));
        Assert.That(data.CurrentHP, Is.EqualTo(999));
        Assert.That(DebugBattleTargetRules.IsDebugTarget(data), Is.True);
    }

    [Test]
    public void TryRestoreFullHp_RestoresLivingTarget()
    {
        MonsterRuntimeData data = new("original", null);
        DebugBattleTargetRules.Configure(data);
        data.CurrentHP = 321;

        bool restored = DebugBattleTargetRules.TryRestoreFullHp(data);

        Assert.That(restored, Is.True);
        Assert.That(data.CurrentHP, Is.EqualTo(999));
    }

    [Test]
    public void TryRestoreFullHp_DoesNotReviveDeadTarget()
    {
        MonsterRuntimeData data = new("original", null);
        DebugBattleTargetRules.Configure(data);
        data.CurrentHP = 0;

        bool restored = DebugBattleTargetRules.TryRestoreFullHp(data);

        Assert.That(restored, Is.False);
        Assert.That(data.CurrentHP, Is.Zero);
    }
}

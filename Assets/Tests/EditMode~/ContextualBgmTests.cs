using System.Collections.Generic;
using NUnit.Framework;

public class ContextualBgmTests
{
    [Test]
    public void ResolveFirstConfigured_SkipsMissingHigherPriorityState()
    {
        BgmState? resolved = BattleBgmPolicy.ResolveFirstConfigured(
            new[] { BgmState.Stage1LowHealth, BgmState.Stage1Common },
            state => state == BgmState.Stage1Common);

        Assert.AreEqual(BgmState.Stage1Common, resolved.Value);
    }

    [Test]
    public void ResolveFirstConfigured_ReturnsNullWhenNothingIsConnected()
    {
        BgmState? resolved = BattleBgmPolicy.ResolveFirstConfigured(
            new[] { BgmState.Stage1LowHealth, BgmState.Stage1Common },
            _ => false);

        Assert.IsNull(resolved);
    }

    [TestCase(30, 100, true)]
    [TestCase(31, 100, false)]
    [TestCase(0, 100, false)]
    public void IsLowHealth_UsesInclusiveThresholdAndExcludesDead(int hp, int maxHp, bool expected)
    {
        Assert.AreEqual(expected, BattleBgmPolicy.IsLowHealth(hp, maxHp, 0.3f));
    }

    [Test]
    public void WrapLoopSample_PreservesOvershoot()
    {
        Assert.AreEqual(25, BgmClipData.WrapLoopSample(105, 20, 100));
        Assert.AreEqual(20, BgmClipData.WrapLoopSample(180, 20, 100));
    }

    [TestCase("Stage1", "Common", BattleBgmRoomVariant.Any, "Stage1", "Common", true)]
    [TestCase("Stage1", "Tutorial", BattleBgmRoomVariant.Battle, "Stage1", "Tutorial", true)]
    [TestCase("Stage1", "Tutorial", BattleBgmRoomVariant.Event, "Stage1", "Tutorial", false)]
    public void RoomBinding_MatchesStageTypeAndVariant(
        string bindingStage, string bindingType, BattleBgmRoomVariant variant,
        string stage, string type, bool hasBattleMap)
    {
        Assert.IsTrue(BattleBgmPolicy.MatchesRoom(bindingStage, bindingType, variant, stage, type, hasBattleMap));
    }
}

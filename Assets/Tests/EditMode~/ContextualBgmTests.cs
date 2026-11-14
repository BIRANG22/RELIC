using System.Collections.Generic;
using NUnit.Framework;

public class ContextualBgmTests
{
    [TestCase(false, true)]
    [TestCase(true, true)]
    public void DefaultBattleBgm_IsSkippedWhenResumingUnclearedRoom(
        bool hasUnclearedCurrentNode,
        bool expected)
    {
        Assert.That(
            BattleState.ShouldPlayDefaultBattleBgm(hasUnclearedCurrentNode),
            Is.EqualTo(expected));
    }

    [TestCase(0f, 2f, 1f)]
    [TestCase(1f, 2f, 0.5f)]
    [TestCase(2f, 2f, 0f)]
    [TestCase(3f, 2f, 0f)]
    public void FadeOutMultiplier_ReachesSilenceAtConfiguredDuration(
        float elapsed,
        float duration,
        float expected)
    {
        Assert.That(
            BgmFadePolicy.EvaluateFadeOut(elapsed, duration),
            Is.EqualTo(expected).Within(0.0001f));
    }

    [TestCase(0f, 2f, 4f, 1f, 0f)]
    [TestCase(1f, 2f, 4f, 0.5f, 0.25f)]
    [TestCase(2f, 2f, 4f, 0f, 0.5f)]
    [TestCase(4f, 2f, 4f, 0f, 1f)]
    public void CrossfadeVolumes_FadeOutgoingAndIncomingAtTheSameTime(
        float elapsed,
        float fadeOutDuration,
        float fadeInDuration,
        float expectedOutgoing,
        float expectedIncoming)
    {
        BgmCrossfadePolicy.Evaluate(
            elapsed,
            fadeOutDuration,
            fadeInDuration,
            out float outgoing,
            out float incoming);

        Assert.That(outgoing, Is.EqualTo(expectedOutgoing).Within(0.0001f));
        Assert.That(incoming, Is.EqualTo(expectedIncoming).Within(0.0001f));
    }

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

    [Test]
    public void ResolveBattleState_ArabellaPhaseTwoOverridesLowHealthAndBossRoom()
    {
        BgmState? resolved = BattleBgmPolicy.ResolveBattleState(
            defeatPlaying: false,
            bossIntroPlaying: false,
            arabellaPhaseTwoPlaying: true,
            partyLowHealth: true,
            defeatState: BgmState.Stage1Defeat,
            bossIntroState: BgmState.Stage1BossIntro,
            arabellaPhaseTwoState: BgmState.ArabellaPhase2,
            lowHealthState: BgmState.Stage1LowHealth,
            roomState: BgmState.Stage1Boss,
            isConfigured: _ => true);

        Assert.That(resolved, Is.EqualTo(BgmState.ArabellaPhase2));
    }

    [Test]
    public void ResolveBattleState_MissingArabellaPhaseTwoFallsBackToBossRoom()
    {
        BgmState? resolved = BattleBgmPolicy.ResolveBattleState(
            defeatPlaying: false,
            bossIntroPlaying: false,
            arabellaPhaseTwoPlaying: true,
            partyLowHealth: false,
            defeatState: BgmState.Stage1Defeat,
            bossIntroState: BgmState.Stage1BossIntro,
            arabellaPhaseTwoState: BgmState.ArabellaPhase2,
            lowHealthState: BgmState.Stage1LowHealth,
            roomState: BgmState.Stage1Boss,
            isConfigured: state => state == BgmState.Stage1Boss);

        Assert.That(resolved, Is.EqualTo(BgmState.Stage1Boss));
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

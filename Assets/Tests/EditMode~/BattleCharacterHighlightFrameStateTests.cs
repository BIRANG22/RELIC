using NUnit.Framework;
using Relic.Gameplay.Battle;

public sealed class BattleCharacterHighlightFrameStateTests
{
    [TestCase(true, false, true)]
    [TestCase(false, true, true)]
    [TestCase(false, false, false)]
    public void ShouldSynchronizeSorting_FollowsVisibleHighlightState(
        bool selected,
        bool hovered,
        bool expected)
    {
        BattleCharacterHighlightFrameState state = new(
            selected,
            hovered,
            presentationBlocked: false,
            executionActive: false);

        Assert.That(state.ShouldSynchronizeSorting, Is.EqualTo(expected));
    }

    [TestCase(false, false, false, false, false)]
    [TestCase(true, false, false, false, true)]
    [TestCase(false, true, false, false, true)]
    [TestCase(true, false, false, true, true)]
    [TestCase(false, true, false, true, false)]
    [TestCase(true, true, true, false, false)]
    public void ShouldSynchronizeAnimator_PreservesSelectedAnimationDuringExecution(
        bool selected,
        bool hovered,
        bool presentationBlocked,
        bool executionActive,
        bool expected)
    {
        BattleCharacterHighlightFrameState state = new(
            selected,
            hovered,
            presentationBlocked,
            executionActive);

        Assert.That(state.ShouldSynchronizeAnimator, Is.EqualTo(expected));
    }

    [Test]
    public void ShouldRetryReferenceLookup_WhenOptionalPresenterIsMissingAndIntervalElapsed()
    {
        bool shouldRetry = BattlePresentationReferenceRetryPolicy.ShouldRetry(
            currentFrame: 300,
            nextRetryFrame: 300,
            turnExecutorAvailable: true,
            reservationControllerAvailable: true,
            tutorialPresenterAvailable: false);

        Assert.That(shouldRetry, Is.True);
    }
}

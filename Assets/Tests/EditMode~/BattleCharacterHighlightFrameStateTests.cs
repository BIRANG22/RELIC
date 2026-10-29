using NUnit.Framework;
using Relic.Gameplay.Battle;

public sealed class BattleCharacterHighlightFrameStateTests
{
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

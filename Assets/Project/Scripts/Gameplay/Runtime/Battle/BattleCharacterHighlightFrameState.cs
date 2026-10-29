namespace Relic.Gameplay.Battle
{
    public readonly struct BattleCharacterHighlightFrameState
    {
        public bool Selected { get; }
        public bool Hovered { get; }
        public bool PresentationBlocked { get; }
        public bool ExecutionActive { get; }

        public bool ShouldSynchronizeAnimator =>
            !PresentationBlocked && (Selected || (Hovered && !ExecutionActive));

        public BattleCharacterHighlightFrameState(
            bool selected,
            bool hovered,
            bool presentationBlocked,
            bool executionActive)
        {
            Selected = selected;
            Hovered = hovered;
            PresentationBlocked = presentationBlocked;
            ExecutionActive = executionActive;
        }
    }

    public static class BattlePresentationReferenceRetryPolicy
    {
        public static bool ShouldRetry(
            int currentFrame,
            int nextRetryFrame,
            bool turnExecutorAvailable,
            bool reservationControllerAvailable,
            bool tutorialPresenterAvailable)
        {
            bool allAvailable = turnExecutorAvailable &&
                                reservationControllerAvailable &&
                                tutorialPresenterAvailable;
            return !allAvailable && currentFrame >= nextRetryFrame;
        }
    }
}

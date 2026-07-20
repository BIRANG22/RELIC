using System;

public sealed class BattleRoomIntroLoadGate
{
    private IBattleRoomIntroSequence pendingSequence;
    private Action pendingLoad;

    public void Request(IBattleRoomIntroSequence sequence, Action load)
    {
        Cancel();

        if (sequence == null || sequence.IsCompleted)
        {
            load?.Invoke();
            return;
        }

        pendingSequence = sequence;
        pendingLoad = load;
        pendingSequence.Completed += HandleCompleted;

        if (pendingSequence.IsCompleted)
            HandleCompleted();
    }

    public void Cancel()
    {
        if (pendingSequence != null)
            pendingSequence.Completed -= HandleCompleted;

        pendingSequence = null;
        pendingLoad = null;
    }

    private void HandleCompleted()
    {
        Action load = pendingLoad;
        Cancel();
        load?.Invoke();
    }
}

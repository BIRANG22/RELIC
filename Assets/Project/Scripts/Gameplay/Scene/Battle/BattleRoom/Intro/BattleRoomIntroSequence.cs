using System;
using UnityEngine;

public abstract class BattleRoomIntroSequence : MonoBehaviour, IBattleRoomIntroSequence
{
    public bool IsCompleted { get; private set; }

    public event Action Completed;

    protected void MarkCompleted()
    {
        if (IsCompleted)
            return;

        IsCompleted = true;
        Completed?.Invoke();
    }

    protected void ResetCompletion()
    {
        IsCompleted = false;
    }
}

using System;

public interface IBattleRoomIntroSequence
{
    bool IsCompleted { get; }
    event Action Completed;
}

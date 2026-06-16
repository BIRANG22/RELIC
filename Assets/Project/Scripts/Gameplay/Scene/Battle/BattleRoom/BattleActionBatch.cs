using System.Collections.Generic;

public class BattleActionBatch
{
    public int TimelineSlotIndex { get; private set; } = -1;

    public readonly List<PlayerReservedCommand> PlayerCommands = new();
    public readonly List<MonsterReservedCommand> MonsterCommands = new();

    public bool HasCommands =>
        PlayerCommands.Count > 0 ||
        MonsterCommands.Count > 0;

    public bool CanAcceptTimelineSlot(int slotIndex)
    {
        return TimelineSlotIndex < 0 || TimelineSlotIndex == slotIndex;
    }

    public void SetTimelineSlotIndexIfNeeded(int slotIndex)
    {
        if (TimelineSlotIndex < 0)
            TimelineSlotIndex = slotIndex;
    }
}

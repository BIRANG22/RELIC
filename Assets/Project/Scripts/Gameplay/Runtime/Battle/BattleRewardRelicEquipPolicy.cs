using System;

public static class BattleRewardRelicEquipPolicy
{
    private const int ActiveRelicSlotIndex = 0;
    private const int FirstPassiveRelicSlotIndex = 1;
    private const int PassiveRelicSlotCount = 6;

    public static int FindFirstEmptySlot(string[] equippedRelicIds, bool activeRelic)
    {
        if (equippedRelicIds == null || equippedRelicIds.Length == 0)
            return -1;

        if (activeRelic)
        {
            return string.IsNullOrWhiteSpace(equippedRelicIds[ActiveRelicSlotIndex])
                ? ActiveRelicSlotIndex
                : -1;
        }

        int endExclusive = Math.Min(
            FirstPassiveRelicSlotIndex + PassiveRelicSlotCount,
            equippedRelicIds.Length);

        for (int slotIndex = FirstPassiveRelicSlotIndex; slotIndex < endExclusive; slotIndex++)
        {
            if (string.IsNullOrWhiteSpace(equippedRelicIds[slotIndex]))
                return slotIndex;
        }

        return -1;
    }
}

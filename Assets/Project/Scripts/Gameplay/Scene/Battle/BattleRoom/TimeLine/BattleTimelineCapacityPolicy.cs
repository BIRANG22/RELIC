using UnityEngine;

public static class BattleTimelineCapacityPolicy
{
    public static int NormalizeOccupiedOrders(int occupiedOrders)
    {
        return Mathf.Max(1, occupiedOrders);
    }

    public static int GetRemainingCapacity(int maximumOrders, int currentOccupiedOrders)
    {
        return Mathf.Max(0, maximumOrders - Mathf.Max(0, currentOccupiedOrders));
    }

    public static bool CanFit(
        int maximumOrders,
        int currentOccupiedOrders,
        int requestedOccupiedOrders)
    {
        return GetRemainingCapacity(maximumOrders, currentOccupiedOrders) >=
               NormalizeOccupiedOrders(requestedOccupiedOrders);
    }
}

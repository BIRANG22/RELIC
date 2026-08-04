using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;

public static class BattleRangeCalculator
{
    public static List<int> GetSelectionRangeIndices(
        int casterGridIndex,
        string rangeId,
        RangeDatabase rangeDatabase,
        GridManager gridManager)
    {
        List<int> result = new();

        if (gridManager == null)
            return result;

        if (IsAllRangeId(rangeId))
            return GetAllGridIndices(gridManager);

        if (rangeDatabase == null || !rangeDatabase.TryGet(rangeId, out SkillRangeData rangeData))
            return result;

        Vector2Int casterCoord = gridManager.IndexToCoord(casterGridIndex);

        foreach (Vector2Int offset in rangeData.Positions)
        {
            Vector2Int targetCoord = casterCoord + offset;

            if (!gridManager.IsValidCoord(targetCoord))
                continue;

            result.Add(gridManager.CoordToIndex(targetCoord));
        }

        return result;
    }

    public static List<int> GetDirectionRangeIndices(
        int casterGridIndex,
        string rangeId,
        BattleDirection direction,
        RangeDatabase rangeDatabase,
        GridManager gridManager)
    {
        List<int> result = new();

        if (gridManager == null)
            return result;

        if (IsAllRangeId(rangeId))
            return GetAllGridIndices(gridManager);

        if (rangeDatabase == null || !rangeDatabase.TryGet(rangeId, out SkillRangeData rangeData))
            return result;

        Vector2Int casterCoord = gridManager.IndexToCoord(casterGridIndex);

        foreach (Vector2Int offset in rangeData.Positions)
        {
            Vector2Int rotatedOffset = BattleDirectionUtility.RotateOffset(offset, direction);
            Vector2Int targetCoord = casterCoord + rotatedOffset;

            if (!gridManager.IsValidCoord(targetCoord))
                continue;

            result.Add(gridManager.CoordToIndex(targetCoord));
        }

        return result;
    }
    public static bool IsAllRangeId(string rangeId)
    {
        return string.Equals(rangeId, "Range_All", System.StringComparison.OrdinalIgnoreCase) ||
               string.Equals(rangeId, "Rnage_All", System.StringComparison.OrdinalIgnoreCase);
    }

    public static List<int> GetAllGridIndices(GridManager gridManager)
    {
        List<int> result = new();

        if (gridManager == null)
            return result;

        int cellCount = gridManager.Width * gridManager.Height;

        for (int index = 0; index < cellCount; index++)
            result.Add(index);

        return result;
    }
}

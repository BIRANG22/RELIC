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

        if (rangeDatabase == null || gridManager == null)
            return result;

        if (!rangeDatabase.TryGet(rangeId, out SkillRangeData rangeData))
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

        if (rangeDatabase == null || gridManager == null)
            return result;

        if (!rangeDatabase.TryGet(rangeId, out SkillRangeData rangeData))
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
}
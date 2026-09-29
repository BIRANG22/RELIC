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
    public static bool IsDirectionalMovePathRangeId(
        string rangeId,
        out bool isAdvance,
        out int maxDistance)
    {
        isAdvance = false;
        maxDistance = 0;

        if (string.IsNullOrWhiteSpace(rangeId))
            return false;

        string trimmed = rangeId.Trim();
        const string advancePrefix = "Range_Advance";
        const string retreatPrefix = "Range_Retreat";
        string distanceText;

        if (trimmed.StartsWith(advancePrefix, System.StringComparison.OrdinalIgnoreCase))
        {
            isAdvance = true;
            distanceText = trimmed.Substring(advancePrefix.Length);
        }
        else if (trimmed.StartsWith(retreatPrefix, System.StringComparison.OrdinalIgnoreCase))
        {
            isAdvance = false;
            distanceText = trimmed.Substring(retreatPrefix.Length);
        }
        else
        {
            return false;
        }

        return int.TryParse(distanceText, out maxDistance) &&
               maxDistance >= 1 &&
               maxDistance <= 6;
    }

    public static bool IsPartyRangeId(string rangeId)
    {
        return string.Equals(rangeId, "Range_Party", System.StringComparison.OrdinalIgnoreCase);
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

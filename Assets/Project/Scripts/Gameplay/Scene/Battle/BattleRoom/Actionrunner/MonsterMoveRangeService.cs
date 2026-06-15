using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    public static class MonsterMoveRangeService
    {
        public static List<Vector2Int> GetMoveOffsets(string rangeId)
        {
            List<Vector2Int> result = new();

            if (string.IsNullOrWhiteSpace(rangeId))
                return result;

            if (DataManager.Instance == null ||
                DataManager.Instance.RangeDatabase == null)
                return result;

            SkillRangeData rangeData =
                DataManager.Instance.RangeDatabase.Get(rangeId);

            if (rangeData == null || rangeData.Positions == null)
                return result;

            for (int i = 0; i < rangeData.Positions.Count; i++)
            {
                Vector2Int offset = rangeData.Positions[i];

                if (offset == Vector2Int.zero)
                    continue;

                result.Add(offset);
            }

            return result;
        }
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    public static class NocturnPortalLandingPolicy
    {
        public static bool TrySelect(
            Vector2Int targetCoord, BattleDirection targetDirection, Func<Vector2Int, bool> canLandAt,
            out Vector2Int landing, out BattleDirection attackDirection)
        {
            bool facesRight = targetDirection == BattleDirection.Right;
            Vector2Int forward = facesRight ? Vector2Int.right : Vector2Int.left;

            landing = targetCoord - forward;
            attackDirection = targetDirection;
            if (canLandAt(landing))
                return true;

            landing = targetCoord + forward;
            attackDirection = facesRight ? BattleDirection.Left : BattleDirection.Right;
            if (canLandAt(landing))
                return true;

            landing = default;
            attackDirection = BattleDirection.Right;
            return false;
        }

        public static bool IsCharacterOccupied(
            int gridIndex, Dictionary<int, int> predictedPositions, Func<int, bool> isCurrentlyOccupied)
        {
            if (predictedPositions == null)
                return isCurrentlyOccupied(gridIndex);

            return predictedPositions.ContainsValue(gridIndex) ||
                   (!predictedPositions.ContainsKey(gridIndex) && isCurrentlyOccupied(gridIndex));
        }
    }
}

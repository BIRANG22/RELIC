using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Battle
{
    public static class MoveDragReservationPolicy
    {
        public static HashSet<int> BuildBlockedGridIndices(
            IEnumerable<int> occupiedUnitIndices,
            IEnumerable<int> blockedGridEffectIndices)
        {
            // Explicit drag paths preserve the player's route. Unit occupancy and
            // blocked grid effects are resolved during execution, not reservation.
            _ = occupiedUnitIndices;
            _ = blockedGridEffectIndices;
            return new HashSet<int>();
        }

        public static bool ShouldRejectOccupiedDestination(
            bool hasExplicitDragPath,
            bool isOccupied)
        {
            return !hasExplicitDragPath && isOccupied;
        }
    }

    public sealed class MoveDragPathState
    {
        private readonly List<Vector2Int> coordinates = new();

        public IReadOnlyList<Vector2Int> Coordinates => coordinates;

        public MoveDragPathState(IEnumerable<Vector2Int> initialCoordinates)
        {
            if (initialCoordinates != null)
                coordinates.AddRange(initialCoordinates);
        }

        public bool TryVisit(Vector2Int coordinate, bool canAppend)
        {
            int previousIndex = coordinates.IndexOf(coordinate);
            if (previousIndex >= 0)
            {
                int removeCount = coordinates.Count - previousIndex - 1;
                if (removeCount <= 0)
                    return false;

                coordinates.RemoveRange(previousIndex + 1, removeCount);
                return true;
            }

            if (!canAppend || coordinates.Count <= 0)
                return false;

            Vector2Int last = coordinates[coordinates.Count - 1];
            Vector2Int delta = coordinate - last;
            if (Mathf.Abs(delta.x) + Mathf.Abs(delta.y) != 1)
                return false;

            coordinates.Add(coordinate);
            return true;
        }
    }
}

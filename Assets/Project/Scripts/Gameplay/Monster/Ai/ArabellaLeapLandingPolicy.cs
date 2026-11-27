using System;
using System.Collections.Generic;

namespace Relic.Gameplay.Monster
{
    // Snapshot-only decision: no scene, rendering, timing or random dependencies.
    // Grid indices use GridManager's column-major convention: x * height + y.
    public static class ArabellaLeapLandingPolicy
    {
        public readonly struct Target
        {
            public readonly int GridIndex;
            public readonly bool FacesRight;
            public readonly bool CanBePushed;
            public Target(int gridIndex, bool facesRight, bool canBePushed)
            {
                GridIndex = gridIndex;
                FacesRight = facesRight;
                CanBePushed = canBePushed;
            }
        }

        public static int Resolve(int width, int height, int origin,
            IReadOnlyList<int> footprint, IReadOnlyList<bool> blocked,
            IReadOnlyList<Target> targets, int preferred = -1, IReadOnlyList<int> followupHits = null)
        {
            if (width <= 0 || height <= 0 || origin < 0 || origin >= width * height ||
                footprint == null || footprint.Count == 0 || blocked == null ||
                blocked.Count != width * height || targets == null)
                return -1;

            // Preserve the telegraphed impact when it remains safe, even if the target dodges.
            if (preferred >= 0 && preferred < blocked.Count && preferred != origin &&
                CanOccupy(preferred, width, height, origin, footprint, blocked) &&
                CountExits(preferred, width, height, origin, footprint, blocked) > 0)
                return preferred;

            int best = -1, bestFailures = int.MaxValue, bestHits = -1, bestExits = -1, bestFollowup = -1;
            int bestDistance = int.MaxValue;
            for (int candidate = 0; candidate < blocked.Count; candidate++)
            {
                if (candidate == origin || !CanOccupy(candidate, width, height, origin, footprint, blocked))
                    continue;
                int x = candidate / height, y = candidate % height;
                int hits = 0, failures = 0;
                foreach (Target target in targets)
                {
                    if (target.GridIndex < 0 || target.GridIndex >= blocked.Count) continue;
                    int tx = target.GridIndex / height, ty = target.GridIndex % height;
                    if (Math.Abs(tx - x) > 1 || Math.Abs(ty - y) > 1) continue;
                    hits++;
                    int pushX = tx + (tx > x ? 1 : tx < x ? -1 : target.FacesRight ? -1 : 1);
                    int push = pushX * height + ty;
                    if (!target.CanBePushed || pushX < 0 || pushX >= width || blocked[push] ||
                        Covers(candidate, push, height, origin, footprint))
                        failures++;
                }
                if (hits == 0) continue;
                int exits = CountExits(candidate, width, height, origin, footprint, blocked);
                // A landing with no legal next move is not a usable AI destination.
                if (exits == 0) continue;
                int followup = followupHits != null && candidate < followupHits.Count ? followupHits[candidate] : 0;
                int distance = Math.Abs(x - origin / height) + Math.Abs(y - origin % height);
                bool better = failures < bestFailures ||
                    (failures == bestFailures && followup > bestFollowup) ||
                    (failures == bestFailures && followup == bestFollowup && hits > bestHits) ||
                    (failures == bestFailures && followup == bestFollowup && hits == bestHits && exits > bestExits) ||
                    (failures == bestFailures && followup == bestFollowup && hits == bestHits && exits == bestExits &&
                        (candidate == preferred || (best != preferred && distance < bestDistance)));
                if (!better) continue;
                best = candidate;
                bestFailures = failures;
                bestHits = hits;
                bestFollowup = followup;
                bestExits = exits;
                bestDistance = distance;
            }
            return best;
        }

        private static int CountExits(int candidate, int width, int height, int origin,
            IReadOnlyList<int> footprint, IReadOnlyList<bool> blocked)
        {
            int x = candidate / height, y = candidate % height, exits = 0;
            if (x > 0 && CanOccupy(candidate - height, width, height, origin, footprint, blocked)) exits++;
            if (x + 1 < width && CanOccupy(candidate + height, width, height, origin, footprint, blocked)) exits++;
            if (y > 0 && CanOccupy(candidate - 1, width, height, origin, footprint, blocked)) exits++;
            if (y + 1 < height && CanOccupy(candidate + 1, width, height, origin, footprint, blocked)) exits++;
            return exits;
        }

        private static bool CanOccupy(int candidate, int width, int height, int origin,
            IReadOnlyList<int> footprint, IReadOnlyList<bool> blocked)
        {
            foreach (int occupied in footprint)
            {
                if (occupied < 0 || occupied >= blocked.Count) return false;
                int x = candidate / height + occupied / height - origin / height;
                int y = candidate % height + occupied % height - origin % height;
                if (x < 0 || x >= width || y < 0 || y >= height || blocked[x * height + y]) return false;
            }
            return true;
        }

        private static bool Covers(int candidate, int grid, int height, int origin, IReadOnlyList<int> footprint)
        {
            foreach (int occupied in footprint)
                if (candidate / height + occupied / height - origin / height == grid / height &&
                    candidate % height + occupied % height - origin % height == grid % height)
                    return true;
            return false;
        }
    }
}

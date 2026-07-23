using System.Collections.Generic;

namespace Relic.Gameplay.Battle
{
    public readonly struct BattleGridEffectPlacement
    {
        public BattleGridEffectPlacement(int gridIndex, string gridEffectId)
        {
            GridIndex = gridIndex;
            GridEffectId = gridEffectId;
        }

        public int GridIndex { get; }
        public string GridEffectId { get; }
    }

    public sealed class BattleGridEffectState
    {
        private readonly Dictionary<int, string> effectIdsByGridIndex = new();
        private readonly Dictionary<int, int> remainingDurationByGridIndex = new();
        private readonly Dictionary<int, int> hitPointsByGridIndex = new();

        public int Count => effectIdsByGridIndex.Count;

        public void Clear()
        {
            effectIdsByGridIndex.Clear();
            remainingDurationByGridIndex.Clear();
            hitPointsByGridIndex.Clear();
        }

        public bool Place(int gridIndex, string gridEffectId, int duration = 0)
        {
            return Place(gridIndex, gridEffectId, duration, 0);
        }

        public bool Place(int gridIndex, string gridEffectId, int duration, int hitPoints)
        {
            if (gridIndex < 0 || string.IsNullOrWhiteSpace(gridEffectId))
                return false;

            if (effectIdsByGridIndex.ContainsKey(gridIndex))
                return false;

            effectIdsByGridIndex.Add(gridIndex, gridEffectId.Trim());

            int safeDuration = System.Math.Max(0, duration);
            if (safeDuration > 0)
                remainingDurationByGridIndex[gridIndex] = safeDuration;

            int safeHitPoints = System.Math.Max(0, hitPoints);
            if (safeHitPoints > 0)
                hitPointsByGridIndex[gridIndex] = safeHitPoints;

            return true;
        }

        public bool Remove(int gridIndex)
        {
            remainingDurationByGridIndex.Remove(gridIndex);
            hitPointsByGridIndex.Remove(gridIndex);
            return effectIdsByGridIndex.Remove(gridIndex);
        }

        public bool TryGetEffectId(int gridIndex, out string gridEffectId)
        {
            return effectIdsByGridIndex.TryGetValue(gridIndex, out gridEffectId);
        }

        public bool TryGetHitPoints(int gridIndex, out int hitPoints)
        {
            return hitPointsByGridIndex.TryGetValue(gridIndex, out hitPoints);
        }

        public bool DamageHitPoints(int gridIndex, int damage, out bool destroyed)
        {
            destroyed = false;

            if (!hitPointsByGridIndex.TryGetValue(gridIndex, out int hitPoints))
                return false;

            int remaining = System.Math.Max(0, hitPoints - System.Math.Max(0, damage));
            hitPointsByGridIndex[gridIndex] = remaining;

            if (remaining > 0)
                return true;

            destroyed = Remove(gridIndex);
            return true;
        }

        public IReadOnlyList<int> AdvanceDurations()
        {
            List<int> expiredGridIndices = new();
            IReadOnlyList<BattleGridEffectPlacement> expiredPlacements = AdvanceDurationsDetailed();

            for (int i = 0; i < expiredPlacements.Count; i++)
                expiredGridIndices.Add(expiredPlacements[i].GridIndex);

            return expiredGridIndices;
        }

        public IReadOnlyList<BattleGridEffectPlacement> AdvanceDurationsDetailed()
        {
            List<BattleGridEffectPlacement> expiredPlacements = new();
            List<int> trackedGridIndices = new(remainingDurationByGridIndex.Keys);

            for (int i = 0; i < trackedGridIndices.Count; i++)
            {
                int gridIndex = trackedGridIndices[i];

                if (!remainingDurationByGridIndex.TryGetValue(gridIndex, out int remaining))
                    continue;

                remaining--;

                if (remaining > 0)
                {
                    remainingDurationByGridIndex[gridIndex] = remaining;
                    continue;
                }

                remainingDurationByGridIndex.Remove(gridIndex);

                if (effectIdsByGridIndex.TryGetValue(gridIndex, out string gridEffectId))
                {
                    Remove(gridIndex);
                    expiredPlacements.Add(new BattleGridEffectPlacement(gridIndex, gridEffectId));
                }
            }

            return expiredPlacements;
        }

        public IReadOnlyList<BattleGridEffectPlacement> GetPlacements()
        {
            List<BattleGridEffectPlacement> placements = new();

            foreach (KeyValuePair<int, string> pair in effectIdsByGridIndex)
                placements.Add(new BattleGridEffectPlacement(pair.Key, pair.Value));

            return placements;
        }
    }
}

using System.Collections.Generic;

namespace Relic.Gameplay.Battle
{
    public readonly struct BattleGridEffectPlacement
    {
        public BattleGridEffectPlacement(
            int gridIndex,
            string gridEffectId,
            string ownerRuntimeId = "",
            string ownerMonsterId = "")
        {
            GridIndex = gridIndex;
            GridEffectId = gridEffectId;
            OwnerRuntimeId = ownerRuntimeId ?? string.Empty;
            OwnerMonsterId = ownerMonsterId ?? string.Empty;
        }

        public int GridIndex { get; }
        public string GridEffectId { get; }
        public string OwnerRuntimeId { get; }
        public string OwnerMonsterId { get; }
    }

    public static class BattleGridEffectDamagePolicy
    {
        public static bool CanMonsterDamage(string ownerRuntimeId, string attackerRuntimeId)
        {
            return string.IsNullOrWhiteSpace(ownerRuntimeId) ||
                   string.IsNullOrWhiteSpace(attackerRuntimeId) ||
                   !string.Equals(ownerRuntimeId, attackerRuntimeId, System.StringComparison.Ordinal);
        }
    }

    public sealed class BattleGridEffectState
    {
        private readonly Dictionary<int, string> effectIdsByGridIndex = new();
        private readonly Dictionary<int, int> remainingDurationByGridIndex = new();
        private readonly Dictionary<int, int> hitPointsByGridIndex = new();
        private readonly Dictionary<int, string> ownerRuntimeIdsByGridIndex = new();
        private readonly Dictionary<int, string> ownerMonsterIdsByGridIndex = new();

        public int Count => effectIdsByGridIndex.Count;

        public void Clear()
        {
            effectIdsByGridIndex.Clear();
            remainingDurationByGridIndex.Clear();
            hitPointsByGridIndex.Clear();
            ownerRuntimeIdsByGridIndex.Clear();
            ownerMonsterIdsByGridIndex.Clear();
        }

        public bool Place(int gridIndex, string gridEffectId, int duration = 0)
        {
            return Place(gridIndex, gridEffectId, duration, 0);
        }

        public bool Place(int gridIndex, string gridEffectId, int duration, int hitPoints)
        {
            return Place(gridIndex, gridEffectId, duration, hitPoints, string.Empty, string.Empty);
        }

        public bool Place(
            int gridIndex,
            string gridEffectId,
            int duration,
            int hitPoints,
            string ownerRuntimeId,
            string ownerMonsterId)
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

            if (!string.IsNullOrWhiteSpace(ownerRuntimeId))
                ownerRuntimeIdsByGridIndex[gridIndex] = ownerRuntimeId.Trim();
            if (!string.IsNullOrWhiteSpace(ownerMonsterId))
                ownerMonsterIdsByGridIndex[gridIndex] = ownerMonsterId.Trim();

            return true;
        }

        public bool RefreshDuration(int gridIndex, int duration)
        {
            if (!effectIdsByGridIndex.ContainsKey(gridIndex))
                return false;

            int safeDuration = System.Math.Max(0, duration);

            if (safeDuration > 0)
                remainingDurationByGridIndex[gridIndex] = safeDuration;
            else
                remainingDurationByGridIndex.Remove(gridIndex);

            return true;
        }

        public bool Remove(int gridIndex)
        {
            remainingDurationByGridIndex.Remove(gridIndex);
            hitPointsByGridIndex.Remove(gridIndex);
            ownerRuntimeIdsByGridIndex.Remove(gridIndex);
            ownerMonsterIdsByGridIndex.Remove(gridIndex);
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

        public bool TryGetOwner(int gridIndex, out string ownerRuntimeId, out string ownerMonsterId)
        {
            ownerRuntimeIdsByGridIndex.TryGetValue(gridIndex, out ownerRuntimeId);
            ownerMonsterIdsByGridIndex.TryGetValue(gridIndex, out ownerMonsterId);
            ownerRuntimeId ??= string.Empty;
            ownerMonsterId ??= string.Empty;
            return !string.IsNullOrWhiteSpace(ownerRuntimeId) || !string.IsNullOrWhiteSpace(ownerMonsterId);
        }

        /// <summary>
        /// 지속시간이 있는 그리드 효과의 현재 남은 턴을 반환합니다.
        /// 지속시간이 0인 영구 효과는 false를 반환합니다.
        /// </summary>
        public bool TryGetRemainingDuration(int gridIndex, out int remainingDuration)
        {
            return remainingDurationByGridIndex.TryGetValue(gridIndex, out remainingDuration);
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
                    TryGetOwner(gridIndex, out string ownerRuntimeId, out string ownerMonsterId);
                    Remove(gridIndex);
                    expiredPlacements.Add(new BattleGridEffectPlacement(
                        gridIndex, gridEffectId, ownerRuntimeId, ownerMonsterId));
                }
            }

            return expiredPlacements;
        }

        public IReadOnlyList<BattleGridEffectPlacement> GetPlacements()
        {
            List<BattleGridEffectPlacement> placements = new();

            foreach (KeyValuePair<int, string> pair in effectIdsByGridIndex)
            {
                TryGetOwner(pair.Key, out string ownerRuntimeId, out string ownerMonsterId);
                placements.Add(new BattleGridEffectPlacement(
                    pair.Key, pair.Value, ownerRuntimeId, ownerMonsterId));
            }

            return placements;
        }
    }
    public static class VespaExecutionDirectionPolicy
    {
        public static BattleDirection Resolve(
            BattleDirection reservedDirection,
            bool reservedDirectionHasTarget,
            bool oppositeDirectionHasTarget)
        {
            if (!reservedDirectionHasTarget && oppositeDirectionHasTarget)
            {
                return reservedDirection == BattleDirection.Right
                    ? BattleDirection.Left
                    : BattleDirection.Right;
            }

            return reservedDirection;
        }
    }
}

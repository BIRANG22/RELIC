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

        public int Count => effectIdsByGridIndex.Count;

        public void Clear()
        {
            effectIdsByGridIndex.Clear();
        }

        public bool Place(int gridIndex, string gridEffectId)
        {
            if (gridIndex < 0 || string.IsNullOrWhiteSpace(gridEffectId))
                return false;

            if (effectIdsByGridIndex.ContainsKey(gridIndex))
                return false;

            effectIdsByGridIndex.Add(gridIndex, gridEffectId.Trim());
            return true;
        }

        public bool Remove(int gridIndex)
        {
            return effectIdsByGridIndex.Remove(gridIndex);
        }

        public bool TryGetEffectId(int gridIndex, out string gridEffectId)
        {
            return effectIdsByGridIndex.TryGetValue(gridIndex, out gridEffectId);
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

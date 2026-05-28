using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public class BattleMapDatabase
    {
        private readonly List<BattleMapData> battleMaps = new();
        private readonly Dictionary<string, List<BattleMapData>> byBattleMapId = new();

        public void Initialize(IEnumerable<BattleMapData> list)
        {
            battleMaps.Clear();
            battleMaps.AddRange(list);

            byBattleMapId.Clear();

            foreach (var data in battleMaps)
            {
                if (data == null || string.IsNullOrWhiteSpace(data.BattleMapId))
                    continue;

                string id = data.BattleMapId.Trim();

                if (!byBattleMapId.TryGetValue(id, out var spawns))
                {
                    spawns = new List<BattleMapData>();
                    byBattleMapId[id] = spawns;
                }

                spawns.Add(data);
            }
        }

        public IReadOnlyList<BattleMapData> GetSpawns(string battleMapId)
        {
            if (string.IsNullOrWhiteSpace(battleMapId))
            {
                Debug.LogWarning("[BattleMapDatabase] BattleMapId is null or empty.");
                return System.Array.Empty<BattleMapData>();
            }

            string id = battleMapId.Trim();

            if (byBattleMapId.TryGetValue(id, out var spawns))
                return spawns;

            Debug.LogWarning($"[BattleMapDatabase] No spawns found. BattleMapId: {id}");
            return System.Array.Empty<BattleMapData>();
        }
    }
}
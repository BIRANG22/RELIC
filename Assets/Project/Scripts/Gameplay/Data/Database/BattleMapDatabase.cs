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

        public BattleMapData GetDropSettings(string battleMapId)
        {
            IReadOnlyList<BattleMapData> spawns = GetSpawns(battleMapId);

            if (spawns == null || spawns.Count == 0)
                return null;

            for (int i = 0; i < spawns.Count; i++)
            {
                BattleMapData data = spawns[i];

                if (data == null)
                    continue;

                if (data.MemoryDropChance > 0f ||
                    data.RelicCommonChance > 0f ||
                    data.RelicRareChance > 0f ||
                    data.RelicEpicChance > 0f ||
                    data.RelicUniqueChance > 0f ||
                    data.MemoryCommonChance > 0f ||
                    data.MemoryRareChance > 0f ||
                    data.MemoryEpicChance > 0f ||
                    data.MemoryUniqueChance > 0f)
                {
                    return data;
                }
            }

            return spawns[0];
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public class RuneDatabase
    {
        private readonly Dictionary<string, RuneData> map = new();

        public void Initialize(IEnumerable<RuneData> list)
        {
            map.Clear();

            foreach (var data in list)
            {
                if (data == null || string.IsNullOrWhiteSpace(data.RuneId))
                    continue;

                if (map.ContainsKey(data.RuneId))
                {
                    Debug.LogWarning($"[RuneDatabase] ม฿บน RuneId: {data.RuneId}");
                    continue;
                }

                map.Add(data.RuneId, data);
            }

            Debug.Log($"[RuneDatabase] Loaded: {map.Count}");
        }

        public RuneData Get(string runeId)
        {
            map.TryGetValue(runeId, out var data);
            return data;
        }
    }
}
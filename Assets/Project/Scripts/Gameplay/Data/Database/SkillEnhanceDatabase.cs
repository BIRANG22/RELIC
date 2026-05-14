using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public class SkillEnhanceDatabase
    {
        private readonly Dictionary<string, SkillEnhanceData> map = new();

        public void Initialize(IEnumerable<SkillEnhanceData> list)
        {
            map.Clear();

            foreach (var data in list)
            {
                if (data == null || string.IsNullOrWhiteSpace(data.Name))
                    continue;

                string key = MakeKey(data.Name, data.EnhancementLevel);

                if (map.ContainsKey(key))
                {
                    Debug.LogWarning($"[SkillEnhanceDatabase] Áßº¹ Å°: {key}");
                    continue;
                }

                map.Add(key, data);
            }

            Debug.Log($"[SkillEnhanceDatabase] Loaded: {map.Count}");
        }

        public SkillEnhanceData Get(string name, int enhancementLevel)
        {
            map.TryGetValue(MakeKey(name, enhancementLevel), out var data);
            return data;
        }

        private string MakeKey(string name, int level)
        {
            return $"{name}_{level}";
        }
    }
}
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public class MonsterSkillDatabase
    {
        private readonly Dictionary<string, MonsterSkillData> map = new();

        public void Initialize(IEnumerable<MonsterSkillData> list)
        {
            map.Clear();

            foreach (var data in list)
            {
                if (data == null || string.IsNullOrWhiteSpace(data.SkillId))
                    continue;

                if (map.ContainsKey(data.SkillId))
                {
                    Debug.LogWarning($"[MonsterSkillDatabase] ม฿บน SkillId: {data.SkillId}");
                    continue;
                }

                map.Add(data.SkillId, data);
            }
        }

        public MonsterSkillData Get(string skillId)
        {
            map.TryGetValue(skillId, out var data);
            return data;
        }
    }
}
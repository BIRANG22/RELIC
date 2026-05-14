using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    [CreateAssetMenu(menuName = "Relic/Data/Skill Icon Database")]
    public class SkillIconDatabase : ScriptableObject
    {
        [SerializeField] private List<SkillIconEntry> entries = new();

        private Dictionary<string, Sprite> map;

        public void Initialize()
        {
            map = new Dictionary<string, Sprite>();

            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.SkillId) || entry.Icon == null)
                    continue;

                var key = entry.SkillId.Trim();

                map[key] = entry.Icon;

                Debug.Log($"[SkillIconDatabase] ADD key='{key}'");
            }
        }

        public bool TryGetIcon(string skillId, out Sprite icon)
        {
            if (map == null)
                Initialize();

            var key = skillId?.Trim();

            Debug.Log($"[SkillIconDatabase] TRY key='{key}'");

            return map.TryGetValue(key, out icon);
        }
    }

    [Serializable]
    public class SkillIconEntry
    {
        public string SkillId;
        public Sprite Icon;
    }
}
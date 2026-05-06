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

                map[entry.SkillId] = entry.Icon;
            }
        }

        public bool TryGetIcon(string skillId, out Sprite icon)
        {
            if (map == null)
                Initialize();

            return map.TryGetValue(skillId, out icon);
        }
    }

    [Serializable]
    public class SkillIconEntry
    {
        public string SkillId;
        public Sprite Icon;
    }
}
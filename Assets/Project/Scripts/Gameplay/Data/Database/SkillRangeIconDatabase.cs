using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    [CreateAssetMenu(menuName = "Relic/Data/Skill Range Icon Database")]
    public class SkillRangeIconDatabase : ScriptableObject
    {
        [SerializeField] private List<SkillRangeIconEntry> entries = new();

        private Dictionary<string, Sprite> map;

        public void Initialize()
        {
            map = new Dictionary<string, Sprite>();

            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.RangeId) || entry.Icon == null)
                    continue;

                map[entry.RangeId] = entry.Icon;
            }
        }

        public bool TryGetIcon(string rangeId, out Sprite icon)
        {
            if (map == null)
                Initialize();

            return map.TryGetValue(rangeId, out icon);
        }
    }

    [Serializable]
    public class SkillRangeIconEntry
    {
        public string RangeId;
        public Sprite Icon;
    }
}
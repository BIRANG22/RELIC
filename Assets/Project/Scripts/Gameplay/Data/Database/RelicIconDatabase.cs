using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    [CreateAssetMenu(menuName = "Relic/Data/Relic Icon Database")]
    public class RelicIconDatabase : ScriptableObject
    {
        [SerializeField] private List<RelicIconEntry> entries = new();

        private Dictionary<string, Sprite> map;

        public void Initialize()
        {
            map = new Dictionary<string, Sprite>();

            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.RelicId) ||
                    entry.Icon == null)
                    continue;

                map[entry.RelicId] = entry.Icon;
            }
        }

        public bool TryGetIcon(string relicId, out Sprite icon)
        {
            icon = null;

            if (map == null)
                Initialize();

            if (string.IsNullOrWhiteSpace(relicId))
                return false;

            return map.TryGetValue(relicId, out icon);
        }
    }

    [Serializable]
    public class RelicIconEntry
    {
        public string RelicId;
        public Sprite Icon;
    }
}
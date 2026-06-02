using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    [CreateAssetMenu(menuName = "Relic/Data/Map Node Icon Database")]
    public class MapNodeIconDatabase : ScriptableObject
    {
        [SerializeField] private List<MapNodeIconEntry> entries = new();

        private Dictionary<string, Sprite> map;

        public void Initialize()
        {
            map = new Dictionary<string, Sprite>();

            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.Type) || entry.Icon == null)
                    continue;

                map[entry.Type] = entry.Icon;
            }
        }

        public bool TryGetIcon(string type, out Sprite icon)
        {
            if (map == null)
                Initialize();

            return map.TryGetValue(type, out icon);
        }
    }

    [Serializable]
    public class MapNodeIconEntry
    {
        public string Type;
        public Sprite Icon;
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    [CreateAssetMenu(menuName = "Relic/Data/Monster Icon Database")]
    public class MonsterIconDatabase : ScriptableObject
    {
        [SerializeField] private List<MonsterIconEntry> entries = new();

        private Dictionary<string, MonsterIconEntry> map;

        public void Initialize()
        {
            map = new Dictionary<string, MonsterIconEntry>();

            foreach (var entry in entries)
            {
                if (entry == null)
                    continue;

                if (string.IsNullOrWhiteSpace(entry.MonsterId))
                    continue;

                map[entry.MonsterId] = entry;
            }
        }

        public bool TryGetIcon(string monsterId, out Sprite icon)
        {
            icon = null;

            if (map == null)
                Initialize();

            if (!map.TryGetValue(monsterId, out var entry))
                return false;

            icon = entry.Icon;
            return icon != null;
        }

        public bool TryGetTimelineIcon(string monsterId, out Sprite icon)
        {
            icon = null;

            if (map == null)
                Initialize();

            if (!map.TryGetValue(monsterId, out var entry))
                return false;

            icon = entry.TimelineIcon;

            if (icon == null)
                icon = entry.Icon;

            return icon != null;
        }
    }

    [Serializable]
    public class MonsterIconEntry
    {
        public string MonsterId;
        public Sprite Icon;
        public Sprite TimelineIcon;
    }
}

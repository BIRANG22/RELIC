using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    [CreateAssetMenu(menuName = "Relic/Data/Rune Icon Database")]
    public class RuneIconDatabase : ScriptableObject
    {
        [SerializeField] private List<RuneIconEntry> entries = new();

        private Dictionary<string, Sprite> map;

        public void Initialize()
        {
            map = new Dictionary<string, Sprite>();

            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.RuneId) || entry.Icon == null)
                    continue;

                map[entry.RuneId] = entry.Icon;
            }
        }

        public bool TryGetIcon(string runeId, out Sprite icon)
        {
            if (map == null)
                Initialize();

            return map.TryGetValue(runeId, out icon);
        }
    }

    [Serializable]
    public class RuneIconEntry
    {
        public string RuneId;
        public Sprite Icon;
    }
}
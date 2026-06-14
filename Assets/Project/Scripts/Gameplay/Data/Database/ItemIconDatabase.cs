using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    [CreateAssetMenu(menuName = "Relic/Data/Item Icon Database")]
    public class ItemIconDatabase : ScriptableObject
    {
        [SerializeField] private List<ItemIconEntry> entries = new();

        private Dictionary<string, Sprite> map;

        public void Initialize()
        {
            map = new Dictionary<string, Sprite>();

            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.ItemId) || entry.Icon == null)
                    continue;

                map[entry.ItemId] = entry.Icon;
            }
        }

        public bool TryGetIcon(string itemId, out Sprite icon)
        {
            if (map == null)
                Initialize();

            return map.TryGetValue(itemId, out icon);
        }
    }

    [Serializable]
    public class ItemIconEntry
    {
        public string ItemId;
        public Sprite Icon;
    }
}
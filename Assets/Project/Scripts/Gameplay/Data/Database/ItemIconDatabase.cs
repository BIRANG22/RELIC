using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    [CreateAssetMenu(menuName = "Relic/Data/Item Icon Database")]
    public class ItemIconDatabase : ScriptableObject
    {
        [SerializeField] private List<ItemIconEntry> entries = new();

        private Dictionary<string, ItemIconEntry> map;

        public void Initialize()
        {
            map = new Dictionary<string, ItemIconEntry>();

            foreach (var entry in entries)
            {
                if (entry == null ||
                    string.IsNullOrWhiteSpace(entry.ItemId) ||
                    (entry.Icon == null && entry.ResearchResultIcon == null))
                {
                    continue;
                }

                map[entry.ItemId] = entry;
            }
        }

        public bool TryGetIcon(string itemId, out Sprite icon)
        {
            icon = null;

            if (map == null)
                Initialize();

            if (string.IsNullOrWhiteSpace(itemId))
                return false;

            if (!map.TryGetValue(itemId, out ItemIconEntry entry) || entry.Icon == null)
                return false;

            icon = entry.Icon;
            return true;
        }

        public bool TryGetResearchResultIcon(string itemId, out Sprite icon)
        {
            icon = null;

            if (map == null)
                Initialize();

            if (string.IsNullOrWhiteSpace(itemId))
                return false;

            if (!map.TryGetValue(itemId, out ItemIconEntry entry) || entry.ResearchResultIcon == null)
                return false;

            icon = entry.ResearchResultIcon;
            return true;
        }
    }

    [Serializable]
    public class ItemIconEntry
    {
        public string ItemId;
        public Sprite Icon;
        public Sprite ResearchResultIcon;
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    [CreateAssetMenu(menuName = "Relic/Data/Action Type Icon Database")]
    public class ActionTypeIconDatabase : ScriptableObject
    {
        [SerializeField] private List<ActionTypeIconEntry> entries = new();

        private Dictionary<string, Sprite> map;

        public void Initialize()
        {
            map = new Dictionary<string, Sprite>();

            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.ActionType) || entry.Icon == null)
                    continue;

                map[entry.ActionType] = entry.Icon;
            }
        }

        public bool TryGetIcon(string actionType, out Sprite icon)
        {
            if (map == null)
                Initialize();

            return map.TryGetValue(actionType, out icon);
        }
    }

    [Serializable]
    public class ActionTypeIconEntry
    {
        public string ActionType;
        public Sprite Icon;
    }
}
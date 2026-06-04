using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    [CreateAssetMenu(menuName = "Relic/Data/Status Effect Icon Database")]
    public class StatusEffectIconDatabase : ScriptableObject
    {
        [SerializeField] private List<StatusEffectIconEntry> entries = new();

        private Dictionary<string, Sprite> map;

        public void Initialize()
        {
            map = new Dictionary<string, Sprite>();

            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.EffectId) || entry.Icon == null)
                    continue;

                map[entry.EffectId] = entry.Icon;
            }
        }

        public bool TryGetIcon(string effectId, out Sprite icon)
        {
            if (map == null)
                Initialize();

            return map.TryGetValue(effectId, out icon);
        }
    }

    [Serializable]
    public class StatusEffectIconEntry
    {
        public string EffectId;
        public Sprite Icon;
    }
}
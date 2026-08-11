using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    [CreateAssetMenu(menuName = "Relic/Data/Status Effect Icon Database")]
    public class StatusEffectIconDatabase : ScriptableObject
    {
        [SerializeField] private Sprite beneficialIcon;
        [SerializeField] private Sprite harmfulIcon;
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

        public bool TryGetTypeIcon(string effectId, EffectDatabase effectDatabase, out Sprite icon)
        {
            icon = null;

            if (!string.IsNullOrWhiteSpace(effectId) &&
                effectDatabase != null &&
                effectDatabase.TryGet(effectId, out EffectMasterData effect) &&
                effect != null)
            {
                return TryGetTypeIcon(effect.EffectType, out icon);
            }

            return false;
        }

        private bool TryGetTypeIcon(EffectType effectType, out Sprite icon)
        {
            icon = null;

            switch (effectType)
            {
                case EffectType.Beneficial:
                    icon = beneficialIcon;
                    return icon != null;

                case EffectType.Harmful:
                    icon = harmfulIcon;
                    return icon != null;

                case EffectType.Neutral:
                default:
                    return false;
            }
        }
    }

    [Serializable]
    public class StatusEffectIconEntry
    {
        public string EffectId;
        public Sprite Icon;
    }
}

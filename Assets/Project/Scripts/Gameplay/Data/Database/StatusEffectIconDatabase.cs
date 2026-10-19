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
        [Header("Icon Image Colors")]
        [SerializeField] private Color innateTraitColor = Color.white;
        [SerializeField] private Color beneficialColor = Color.white;
        [SerializeField] private Color harmfulColor = Color.white;

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

        /// <summary>
        /// 몬스터 고유 특성(Arrow 비표시) 또는 효과 유형에 따라 IconImage 색상을 반환합니다.
        /// 데이터에 유형이 없는 경우 기본 흰색을 사용합니다.
        /// </summary>
        public Color GetIconColor(string effectId, EffectDatabase effectDatabase, bool isMonsterInnateTrait)
        {
            if (isMonsterInnateTrait)
                return innateTraitColor;

            if (!string.IsNullOrWhiteSpace(effectId) &&
                effectDatabase != null &&
                effectDatabase.TryGet(effectId, out EffectMasterData effect) &&
                effect != null)
            {
                switch (effect.EffectType)
                {
                    case EffectType.Beneficial:
                        return beneficialColor;
                    case EffectType.Harmful:
                        return harmfulColor;
                }
            }

            return Color.white;
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

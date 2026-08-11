using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    [CreateAssetMenu(menuName = "Relic/Data/Skill Icon Database")]
    public class SkillIconDatabase : ScriptableObject
    {
        private const string LegacyPublicPrefix = "S_Public_";
        private const string CorePrefix = "S_Core_";

        [SerializeField] private List<SkillIconEntry> entries = new();

        private Dictionary<string, Sprite> map;

        public void Initialize()
        {
            map = new Dictionary<string, Sprite>();

            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.SkillId) || entry.Icon == null)
                    continue;

                map[entry.SkillId] = entry.Icon;
            }
        }

        public bool TryGetIcon(string skillId, out Sprite icon)
        {
            if (map == null)
                Initialize();

            if (string.IsNullOrWhiteSpace(skillId))
            {
                icon = null;
                return false;
            }

            skillId = skillId.Trim();

            if (map.TryGetValue(skillId, out icon))
                return true;

            // 기존 공용 스킬 01~20은 코어 스킬 61~80으로 이전되었습니다.
            // 이전 ID가 남아 있는 데이터도 새 코어 스킬 아이콘을 사용하도록 연결합니다.
            if (TryGetLegacyPublicCoreId(skillId, out string migratedCoreSkillId))
            {
                return map.TryGetValue(migratedCoreSkillId, out icon);
            }

            icon = null;
            return false;
        }

        private static bool TryGetLegacyPublicCoreId(string skillId, out string migratedCoreSkillId)
        {
            migratedCoreSkillId = null;

            if (!skillId.StartsWith(LegacyPublicPrefix, StringComparison.OrdinalIgnoreCase))
                return false;

            string numberText = skillId.Substring(LegacyPublicPrefix.Length);
            if (!int.TryParse(numberText, out int number) || number < 1 || number > 20)
                return false;

            migratedCoreSkillId = CorePrefix + (number + 60).ToString("D2");
            return true;
        }
    }

    [Serializable]
    public class SkillIconEntry
    {
        public string SkillId;
        public Sprite Icon;
    }
}

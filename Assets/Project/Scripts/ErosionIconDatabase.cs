using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    /// <summary>
    /// Erosion 데이터의 GroupId와 UI 아이콘을 연결하는 공용 에셋 DB입니다.
    /// 로비와 전투씬은 모두 이 DB를 동일한 아이콘 원본으로 사용합니다.
    /// </summary>
    [CreateAssetMenu(menuName = "Relic/Data/Erosion Icon Database")]
    public class ErosionIconDatabase : ScriptableObject
    {
        [SerializeField] private Sprite unavailableIcon;
        [SerializeField] private List<ErosionIconEntry> entries = new();

        private Dictionary<string, Sprite> map;

        public Sprite UnavailableIcon => unavailableIcon;

        private void OnEnable()
        {
            // Domain Reload가 꺼져 있거나 에셋 내용이 변경된 경우에도
            // 이전 Dictionary가 남지 않도록 활성화될 때마다 다시 구성합니다.
            Initialize();
        }

        public void Initialize()
        {
            map = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

            foreach (ErosionIconEntry entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.GroupId) || entry.Icon == null)
                    continue;

                string key = NormalizeKey(entry.GroupId);
                if (!string.IsNullOrEmpty(key))
                    map[key] = entry.Icon;
            }
        }

        public bool TryGetIcon(string groupId, out Sprite icon)
        {
            icon = null;
            string key = NormalizeKey(groupId);
            if (string.IsNullOrEmpty(key))
                return false;

            // ScriptableObject 에셋을 Inspector에서 수정했거나 Domain Reload가 꺼져 있어도
            // 항상 현재 Entries를 기준으로 조회되도록 Dictionary를 갱신합니다.
            Initialize();

            if (map.TryGetValue(key, out icon) && icon != null)
                return true;

            // 혹시 Dictionary 캐시와 직렬화 리스트 상태가 어긋난 경우에도
            // 실제 에셋 Entries를 직접 확인해 아이콘을 놓치지 않습니다.
            foreach (ErosionIconEntry entry in entries)
            {
                if (entry == null || entry.Icon == null)
                    continue;

                if (string.Equals(NormalizeKey(entry.GroupId), key, StringComparison.OrdinalIgnoreCase))
                {
                    icon = entry.Icon;
                    return true;
                }
            }

            return false;
        }

        public int EntryCount => entries != null ? entries.Count : 0;

        public bool ContainsKey(string key)
        {
            return TryGetIcon(key, out _);
        }

        /// <summary>
        /// ErosionData의 실제 키를 우선 사용하고, 개별 아이콘이 없으면
        /// Erosion_03_08 -> Group_03 형식의 공용 그룹 아이콘으로 대체합니다.
        /// </summary>
        public bool TryGetIcon(ErosionData data, out Sprite icon)
        {
            icon = null;
            if (data == null)
                return false;

            // 1. 데이터에 기록된 GroupId와 정확히 일치하는 키.
            if (TryGetIcon(data.GroupId, out icon))
                return true;

            // 2. DifficultyId 자체를 키로 등록한 개별 침식도 아이콘.
            string difficultyId = NormalizeKey(data.DifficultyId);
            if (!string.IsNullOrEmpty(difficultyId) && TryGetIcon(difficultyId, out icon))
                return true;

            // 3. Erosion_03_08 -> Group_03 형태의 공용 그룹 아이콘.
            string groupFallback = BuildGroupFallbackKey(difficultyId);
            if (!string.IsNullOrEmpty(groupFallback) && TryGetIcon(groupFallback, out icon))
                return true;

            // GroupId가 Erosion_XX_YY 형식으로 들어오는 데이터도 동일하게 처리합니다.
            string normalizedGroupId = NormalizeKey(data.GroupId);
            groupFallback = BuildGroupFallbackKey(normalizedGroupId);
            return !string.IsNullOrEmpty(groupFallback) && TryGetIcon(groupFallback, out icon);
        }

        private static string BuildGroupFallbackKey(string erosionKey)
        {
            if (string.IsNullOrEmpty(erosionKey))
                return string.Empty;

            // 기대 형식: Erosion_03_08 또는 Erosion_03
            string[] parts = erosionKey.Split('_');
            if (parts.Length < 2 || !parts[0].Equals("Erosion", StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            if (!int.TryParse(parts[1], out int groupNumber))
                return string.Empty;

            return $"Group_{groupNumber:00}";
        }

        private static string NormalizeKey(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    [Serializable]
    public class ErosionIconEntry
    {
        public string GroupId;
        public Sprite Icon;
    }
}

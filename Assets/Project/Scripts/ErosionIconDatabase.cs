using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    /// <summary>
    /// Erosion GroupId와 UI 아이콘을 연결하는 에셋 DB입니다.
    /// 선택 불가 슬롯은 Unavailable Icon을 공통으로 사용할 수 있습니다.
    /// </summary>
    [CreateAssetMenu(menuName = "Relic/Data/Erosion Icon Database")]
    public class ErosionIconDatabase : ScriptableObject
    {
        [SerializeField] private Sprite unavailableIcon;
        [SerializeField] private List<ErosionIconEntry> entries = new();

        private Dictionary<string, Sprite> map;

        public Sprite UnavailableIcon => unavailableIcon;

        public void Initialize()
        {
            map = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

            foreach (ErosionIconEntry entry in entries)
            {
                if (entry == null ||
                    string.IsNullOrWhiteSpace(entry.GroupId) ||
                    entry.Icon == null)
                {
                    continue;
                }

                map[entry.GroupId.Trim()] = entry.Icon;
            }
        }

        public bool TryGetIcon(string groupId, out Sprite icon)
        {
            icon = null;

            if (map == null)
                Initialize();

            return !string.IsNullOrWhiteSpace(groupId) &&
                   map.TryGetValue(groupId.Trim(), out icon) &&
                   icon != null;
        }
    }

    [Serializable]
    public class ErosionIconEntry
    {
        public string GroupId;
        public Sprite Icon;
    }
}

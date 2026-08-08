using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GameData MonsterSkill의 SkillIcon 문자열 키를 실제 Sprite와 연결합니다.
/// </summary>
[CreateAssetMenu(fileName = "MonsterSkillIconDatabase", menuName = "Relic/Database/Monster Skill Icon Database")]
public class MonsterSkillIconDatabase : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        [Tooltip("GameData MonsterSkill.SkillIcon 값입니다. 예: icon_mon_move")]
        public string iconId;
        public Sprite sprite;
    }

    [SerializeField] private List<Entry> entries = new();

    private Dictionary<string, Sprite> iconMap;

    private void OnEnable()
    {
        RebuildCache();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        RebuildCache();
    }
#endif

    public bool TryGetIcon(string iconId, out Sprite sprite)
    {
        sprite = null;

        if (string.IsNullOrWhiteSpace(iconId))
            return false;

        if (iconMap == null)
            RebuildCache();

        return iconMap.TryGetValue(iconId.Trim(), out sprite) && sprite != null;
    }

    private void RebuildCache()
    {
        iconMap = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

        if (entries == null)
            return;

        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.iconId) || entry.sprite == null)
                continue;

            iconMap[entry.iconId.Trim()] = entry.sprite;
        }
    }
}

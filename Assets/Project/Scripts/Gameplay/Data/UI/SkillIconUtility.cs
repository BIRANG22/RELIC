using UnityEngine;
using Relic.Gameplay.Data;

public static class SkillIconUtility
{
    public static Sprite GetSkillIcon(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId))
            return null;

        if (DataManager.Instance == null)
            return null;

        if (DataManager.Instance.SkillIconDatabase == null)
            return null;

        if (DataManager.Instance.SkillIconDatabase.TryGetIcon(skillId, out var icon))
            return icon;

        return null;
    }
}
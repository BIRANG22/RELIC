using System;
using UnityEngine;

[Serializable]
public class StatusVfxDatabaseEntry
{
    [Tooltip("상태 효과 ID를 입력합니다. 예: Aiming, Addicted")]
    public string EffectId;

    [Tooltip("해당 상태 효과에 재생할 VFX입니다.")]
    public BattleVfxEntry Vfx = new();
}

[CreateAssetMenu(
    fileName = "StatusVfxDatabase",
    menuName = "Relic/Battle/Status VFX Database")]
public class StatusVfxDatabase : ScriptableObject
{
    [Header("Buff")]
    [SerializeField] private StatusVfxDatabaseEntry[] buffEntries = Array.Empty<StatusVfxDatabaseEntry>();

    [Header("Debuff")]
    [SerializeField] private StatusVfxDatabaseEntry[] debuffEntries = Array.Empty<StatusVfxDatabaseEntry>();

    public bool TryGetVfx(string effectId, out BattleVfxEntry vfx)
    {
        vfx = null;

        if (string.IsNullOrWhiteSpace(effectId))
            return false;

        string targetId = effectId.Trim();

        if (TryGetFromEntries(buffEntries, targetId, out vfx))
            return true;

        return TryGetFromEntries(debuffEntries, targetId, out vfx);
    }

    public BattleVfxEntry GetVfx(string effectId)
    {
        return TryGetVfx(effectId, out BattleVfxEntry vfx) ? vfx : null;
    }

    private static bool TryGetFromEntries(
        StatusVfxDatabaseEntry[] entries,
        string effectId,
        out BattleVfxEntry vfx)
    {
        vfx = null;

        if (entries == null)
            return false;

        for (int i = 0; i < entries.Length; i++)
        {
            StatusVfxDatabaseEntry entry = entries[i];

            if (entry == null || string.IsNullOrWhiteSpace(entry.EffectId))
                continue;

            if (!string.Equals(
                    entry.EffectId.Trim(),
                    effectId,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            vfx = entry.Vfx;
            return vfx != null;
        }

        return false;
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    [CreateAssetMenu(menuName = "Relic/Data/Skill VFX Database")]
    public class SkillVfxDatabase : ScriptableObject
    {
        [SerializeField] private List<SkillVfxEntry> entries = new();

        private Dictionary<string, BattleVfxEntry> map;

        public void Initialize()
        {
            map = new Dictionary<string, BattleVfxEntry>();

            foreach (SkillVfxEntry entry in entries)
            {
                if (entry == null)
                    continue;

                string skillId = NormalizeId(entry.SkillId);
                if (string.IsNullOrWhiteSpace(skillId))
                    continue;

                if (entry.Vfx == null || entry.Vfx.prefab == null)
                {
                    Debug.LogWarning($"[SkillVfxDatabase] VFX prefab missing: {skillId}");
                    continue;
                }

                if (map.ContainsKey(skillId))
                {
                    Debug.LogWarning($"[SkillVfxDatabase] Duplicate SkillId: {skillId}");
                    continue;
                }

                map.Add(skillId, entry.Vfx);
            }
        }

        public bool TryGetVfx(string skillId, out BattleVfxEntry vfx)
        {
            vfx = null;
            skillId = NormalizeId(skillId);

            if (string.IsNullOrWhiteSpace(skillId))
                return false;

            if (map == null)
                Initialize();

            return map.TryGetValue(skillId, out vfx) && vfx != null && vfx.prefab != null;
        }

        private static string NormalizeId(string id)
        {
            return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
        }
    }

    [Serializable]
    public class SkillVfxEntry
    {
        public string SkillId;
        public BattleVfxEntry Vfx = new();
    }
}

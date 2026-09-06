using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    [CreateAssetMenu(menuName = "Relic/Data/Skill VFX Database")]
    public class SkillVfxDatabase : ScriptableObject
    {
        [SerializeField] private List<SkillVfxEntry> entries = new();

        private Dictionary<string, SkillVfxEntry> map;

        public IReadOnlyList<SkillVfxEntry> Entries => entries;

        public void Initialize()
        {
            map = new Dictionary<string, SkillVfxEntry>();

            foreach (SkillVfxEntry entry in entries)
            {
                if (entry == null)
                    continue;

                string skillId = NormalizeId(entry.SkillId);
                if (string.IsNullOrWhiteSpace(skillId))
                    continue;

                if (map.ContainsKey(skillId))
                {
                    Debug.LogWarning($"[SkillVfxDatabase] Duplicate SkillId: {skillId}");
                    continue;
                }

                map.Add(skillId, entry);
            }
        }

        public bool TryGetEntry(string skillId, out SkillVfxEntry entry)
        {
            entry = null;
            skillId = NormalizeId(skillId);

            if (string.IsNullOrWhiteSpace(skillId))
                return false;

            if (map == null)
                Initialize();

            return map.TryGetValue(skillId, out entry) && entry != null;
        }

        public bool TryGetVfx(string skillId, out BattleVfxEntry vfx)
        {
            vfx = null;
            skillId = NormalizeId(skillId);

            if (string.IsNullOrWhiteSpace(skillId))
                return false;

            if (!TryGetEntry(skillId, out SkillVfxEntry entry))
                return false;

            vfx = entry.Vfx;
            return vfx != null && vfx.prefab != null;
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
        public BattleProjectileVfxEntry ProjectileVfx = new();

        [Tooltip("스킬 효과를 실제로 받는 유닛 위치에 생성할 VFX. 비워두면 생성하지 않습니다.")]
        public BattleVfxEntry TargetUnitVfx = new();
    }
}

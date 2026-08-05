using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public enum SkillAttackSlot
    {
        None = 0,
        Attack1 = 1,
        Attack2 = 2,
        Attack3 = 3
    }

    [CreateAssetMenu(menuName = "Relic/Data/Skill Attack Override Database")]
    public class SkillAttackOverrideDatabase : ScriptableObject
    {
        [SerializeField] private List<SkillAttackOverrideEntry> entries = new();

        private Dictionary<string, SkillAttackSlot> map;

        public void Initialize()
        {
            map = new Dictionary<string, SkillAttackSlot>();

            foreach (SkillAttackOverrideEntry entry in entries)
            {
                if (entry == null)
                    continue;

                string characterId = NormalizeId(entry.CharacterId);
                string skillId = NormalizeId(entry.SkillId);

                if (string.IsNullOrWhiteSpace(characterId) ||
                    string.IsNullOrWhiteSpace(skillId) ||
                    entry.AttackSlot == SkillAttackSlot.None)
                {
                    continue;
                }

                string key = MakeKey(characterId, skillId);
                if (map.ContainsKey(key))
                {
                    Debug.LogWarning(
                        $"[SkillAttackOverrideDatabase] Duplicate override: {characterId} / {skillId}");
                    continue;
                }

                map.Add(key, entry.AttackSlot);
            }
        }

        public bool TryGetAttackSlot(
            string characterId,
            string skillId,
            out SkillAttackSlot attackSlot)
        {
            attackSlot = SkillAttackSlot.None;

            characterId = NormalizeId(characterId);
            skillId = NormalizeId(skillId);

            if (string.IsNullOrWhiteSpace(characterId) || string.IsNullOrWhiteSpace(skillId))
                return false;

            if (map == null)
                Initialize();

            return map.TryGetValue(MakeKey(characterId, skillId), out attackSlot);
        }

        public bool TryGetAttackIndex(string characterId, string skillId, out int attackIndex)
        {
            attackIndex = 0;

            if (!TryGetAttackSlot(characterId, skillId, out SkillAttackSlot attackSlot))
                return false;

            attackIndex = (int)attackSlot;
            return attackIndex >= 1 && attackIndex <= 3;
        }

        private static string NormalizeId(string id)
        {
            return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
        }

        private static string MakeKey(string characterId, string skillId)
        {
            return $"{characterId}\n{skillId}";
        }
    }

    [Serializable]
    public class SkillAttackOverrideEntry
    {
        public string CharacterId;
        public string SkillId;
        public SkillAttackSlot AttackSlot = SkillAttackSlot.None;
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class MonsterMasterData
    {
        public const int PossibleSkillSlotCount = 10;

        public string MonsterId;
        public string Name;
        public string Grade;
        [FormerlySerializedAs("Health")]
        public int HP;
        public int Armor;

        public int MinRemnant;
        public int MaxRemnant;
        public string UniqueItemId;
        public float UniqueItemChance;
        public float RelicChance;
        public string AttackRangeId;

        public string PossSkillId01;
        public string PossSkillId02;
        public string PossSkillId03;
        public string PossSkillId04;
        public string PossSkillId05;
        public string PossSkillId06;
        public string PossSkillId07;
        public string PossSkillId08;
        public string PossSkillId09;
        public string PossSkillId10;

        [NonSerialized]
        public GameObject BattlePrefab;

        public string[] GetPossibleSkillIdSlots()
        {
            return new[]
            {
                NormalizePossibleSkillId(PossSkillId01),
                NormalizePossibleSkillId(PossSkillId02),
                NormalizePossibleSkillId(PossSkillId03),
                NormalizePossibleSkillId(PossSkillId04),
                NormalizePossibleSkillId(PossSkillId05),
                NormalizePossibleSkillId(PossSkillId06),
                NormalizePossibleSkillId(PossSkillId07),
                NormalizePossibleSkillId(PossSkillId08),
                NormalizePossibleSkillId(PossSkillId09),
                NormalizePossibleSkillId(PossSkillId10)
            };
        }

        public string[] GetPossibleSkillIds()
        {
            List<string> skillIds = new();
            string[] slots = GetPossibleSkillIdSlots();

            for (int i = 0; i < slots.Length; i++)
            {
                if (!string.IsNullOrEmpty(slots[i]))
                    skillIds.Add(slots[i]);
            }

            return skillIds.ToArray();
        }

        public string GetPossibleSkillIdAtActionIndex(int actionIndex)
        {
            if (actionIndex < 1 || actionIndex > PossibleSkillSlotCount)
                return "";

            return GetPossibleSkillIdSlots()[actionIndex - 1];
        }

        public int GetActionIndexForSkill(string skillId)
        {
            string normalizedSkillId = NormalizePossibleSkillId(skillId);

            if (string.IsNullOrEmpty(normalizedSkillId))
                return 0;

            string[] slots = GetPossibleSkillIdSlots();

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == normalizedSkillId)
                    return i + 1;
            }

            return 0;
        }

        private static string NormalizePossibleSkillId(string skillId)
        {
            if (string.IsNullOrWhiteSpace(skillId))
                return "";

            string trimmedSkillId = skillId.Trim();
            return trimmedSkillId == "0" ? "" : trimmedSkillId;
        }
    }
}

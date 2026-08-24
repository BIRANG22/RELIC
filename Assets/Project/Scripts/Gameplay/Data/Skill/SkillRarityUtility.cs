using System;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public static class SkillRarityUtility
    {
        public static bool CanUpgrade(SkillMasterData skill)
        {
            if (skill == null)
                return false;

            return skill.Category == Category.Ability ||
                   skill.Category == Category.Public ||
                   skill.Category == Category.Core;
        }

        public static bool CanUnequip(SkillMasterData skill)
        {
            if (skill == null)
                return false;

            return skill.Category == Category.Public ||
                   skill.Category == Category.Core;
        }

        public static bool CanEquipToFreeSlot(SkillMasterData skill)
        {
            return CanUnequip(skill);
        }

        public static bool IsCoreDropRarity(SkillRarity rarity)
        {
            return rarity == SkillRarity.Common ||
                   rarity == SkillRarity.Rare ||
                   rarity == SkillRarity.Epic ||
                   rarity == SkillRarity.Unique;
        }

        public static bool IsBaseSkillVariant(string skillId)
        {
            if (IsUnpairedNumberedSkill(skillId))
                return true;

            if (!TryGetTrailingNumber(skillId, out int number))
                return true;

            return number % 2 != 0;
        }

        public static bool IsUpgradeSkillVariant(string skillId)
        {
            if (IsUnpairedNumberedSkill(skillId))
                return false;

            return TryGetTrailingNumber(skillId, out int number) && number % 2 == 0;
        }

        public static readonly Color UpgradedSkillIconColor = new Color32(0x7E, 0x93, 0xEC, 0xFF);

        public static Color GetSkillIconColor(string skillId)
        {
            return IsUpgradeSkillVariant(skillId) ? UpgradedSkillIconColor : Color.white;
        }

        public static Color GetSkillIconColor(string skillId, Color normalColor)
        {
            return IsUpgradeSkillVariant(skillId) ? UpgradedSkillIconColor : normalColor;
        }

        public static bool TryGetPairedVariantId(string skillId, out string pairedSkillId)
        {
            pairedSkillId = null;

            if (string.IsNullOrWhiteSpace(skillId))
                return false;

            skillId = skillId.Trim();

            if (IsUnpairedNumberedSkill(skillId))
                return false;

            int underscoreIndex = skillId.LastIndexOf('_');

            if (underscoreIndex < 0 || underscoreIndex >= skillId.Length - 1)
                return false;

            string prefix = skillId.Substring(0, underscoreIndex + 1);
            string numberText = skillId.Substring(underscoreIndex + 1);

            if (!int.TryParse(numberText, out int number))
                return false;

            int pairedNumber = number % 2 == 0 ? number - 1 : number + 1;

            if (pairedNumber <= 0)
                return false;

            pairedSkillId = prefix + pairedNumber.ToString(new string('0', numberText.Length));
            return true;
        }

        public static string GetCanonicalName(SkillRarity rarity)
        {
            if (rarity == SkillRarity.Move) return "Move";
            if (rarity == SkillRarity.Exclusive) return "Exclusive";
            if (rarity == SkillRarity.Common) return "Common";
            if (rarity == SkillRarity.Rare) return "Rare";
            if (rarity == SkillRarity.Epic) return "Epic";
            if (rarity == SkillRarity.Unique) return "Unique";
            return string.Empty;
        }

        public static string GetDisplayName(SkillRarity rarity)
        {
            return rarity switch
            {
                SkillRarity.Move => "이동",
                SkillRarity.Exclusive => "기억",
                SkillRarity.Common => "일반 기억",
                SkillRarity.Rare => "레어 기억",
                SkillRarity.Epic => "에픽 기억",
                SkillRarity.Unique => "유니크 기억",
                _ => string.Empty
            };
        }

        public static string GetDisplayName(SkillMasterData skill)
        {
            return GetMemoryTypeDisplayName(skill);
        }

        public static string GetMemoryTypeDisplayName(SkillMasterData skill)
        {
            if (skill == null)
                return string.Empty;

            return skill.Category switch
            {
                Category.Passive => "본능 기억",
                Category.Unique => "발현 기억",
                Category.Ability => "구현 기억",
                _ => GetDisplayName(skill.Rarity)
            };
        }

        private static bool TryGetTrailingNumber(string id, out int number)
        {
            number = 0;

            if (string.IsNullOrWhiteSpace(id))
                return false;

            int underscoreIndex = id.LastIndexOf('_');

            if (underscoreIndex < 0 || underscoreIndex >= id.Length - 1)
                return false;

            string numberText = id.Substring(underscoreIndex + 1);
            return int.TryParse(numberText, out number);
        }

        private static bool IsUnpairedNumberedSkill(string skillId)
        {
            if (string.IsNullOrWhiteSpace(skillId))
                return false;

            string normalizedSkillId = skillId.Trim();
            return normalizedSkillId.StartsWith("S_Passive_", StringComparison.OrdinalIgnoreCase) ||
                   normalizedSkillId.StartsWith("S_Unique_", StringComparison.OrdinalIgnoreCase);
        }
    }

}

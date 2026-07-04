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
            return rarity == SkillRarity.CoreCommon ||
                   rarity == SkillRarity.CoreRare ||
                   rarity == SkillRarity.CoreEpic;
        }

        public static bool IsBaseSkillVariant(string skillId)
        {
            if (!TryGetTrailingNumber(skillId, out int number))
                return true;

            return number % 2 != 0;
        }

        public static bool IsUpgradeSkillVariant(string skillId)
        {
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

        public static string GetDisplayName(SkillRarity rarity)
        {
            return rarity switch
            {
                SkillRarity.Move => "이동",
                SkillRarity.Passive => "패시브",
                SkillRarity.Unique => "고유",
                SkillRarity.CharacterExclusive => "캐릭터 전용",
                SkillRarity.Shared => "공유 가능",
                SkillRarity.CoreCommon => "코어 일반",
                SkillRarity.CoreRare => "코어 희귀",
                SkillRarity.CoreEpic => "코어 영웅",
                _ => string.Empty
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
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public class CharacterRuntimeStore
    {
        private readonly Dictionary<string, CharacterRuntimeData> map = new();

        public void AddOrUpdate(CharacterRuntimeData data)
        {
            map[data.CharacterId] = data;

            LogCharacterRuntime(data);
        }

        public CharacterRuntimeData Get(string characterId)
        {
            map.TryGetValue(characterId, out var data);
            return data;
        }

        public bool TryGet(string characterId, out CharacterRuntimeData data)
        {
            return map.TryGetValue(characterId, out data);
        }

        public IReadOnlyDictionary<string, CharacterRuntimeData> GetAll()
        {
            return map;
        }

        public void Clear()
        {
            map.Clear();
        }

        public void SetAll(IEnumerable<CharacterRuntimeData> characters)
        {
            Clear();

            if (characters == null)
                return;

            foreach (CharacterRuntimeData character in characters)
            {
                if (character == null || string.IsNullOrWhiteSpace(character.CharacterId))
                    continue;

                map[character.CharacterId] = character;
            }
        }

        public void ResetUpgradedSkillVariantsToBase()
        {
            foreach (CharacterRuntimeData data in map.Values)
            {
                if (data == null)
                    continue;

                data.AbilitySkillId = ConvertUpgradeVariantToBase(data.AbilitySkillId);

                if (data.EquippedSkillIds != null)
                {
                    for (int i = 0; i < data.EquippedSkillIds.Length; i++)
                    {
                        if (i == 0)
                            continue;

                        data.EquippedSkillIds[i] = ConvertUpgradeVariantToBase(data.EquippedSkillIds[i]);
                    }
                }
            }
        }

        private static string ConvertUpgradeVariantToBase(string skillId)
        {
            if (string.IsNullOrWhiteSpace(skillId))
                return skillId;

            string trimmedSkillId = skillId.Trim();

            if (!SkillRarityUtility.IsUpgradeSkillVariant(trimmedSkillId))
                return skillId;

            if (!SkillRarityUtility.TryGetPairedVariantId(trimmedSkillId, out string baseSkillId))
                return skillId;

            return baseSkillId;
        }

        private void LogCharacterRuntime(CharacterRuntimeData data)
        {
            if (data == null)
                return;

            string equippedSkills = data.EquippedSkillIds != null
                ? string.Join(", ", data.EquippedSkillIds)
                : "None";

            string skills =
                $"Move:{data.MoveSkillId}, " +
                $"Passive:{data.PassiveSkillId}, " +
                $"Unique:{data.UniqueSkillId}, " +
                $"Ability:{data.AbilitySkillId}, " +
                $"Equipped:[{equippedSkills}]";

        }
    }
}

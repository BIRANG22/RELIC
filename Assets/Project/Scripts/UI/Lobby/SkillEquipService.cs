using UnityEngine;

namespace Relic.Gameplay.Data
{
    public class SkillEquipService
    {
        private const int MaxSkillCount = 4;
        private readonly CharacterRuntimeStore characterStore;

        public SkillEquipService(CharacterRuntimeStore characterStore)
        {
            this.characterStore = characterStore;
        }

        public bool EquipSkill(string characterId, string skillId)
        {
            if (!characterStore.TryGet(characterId, out var character))
            {
                Debug.LogWarning($"[SkillEquipService] Character runtime not found: {characterId}");
                return false;
            }

            if (character.EquippedSkillIds == null || character.EquippedSkillIds.Length != MaxSkillCount)
            {
                character.EquippedSkillIds = new string[MaxSkillCount];
            }

            for (int i = 0; i < character.EquippedSkillIds.Length; i++)
            {
                if (character.EquippedSkillIds[i] == skillId)
                {
                    Debug.LogWarning($"[SkillEquipService] Skill already equipped: {skillId}");
                    return false;
                }
            }

            for (int i = 0; i < character.EquippedSkillIds.Length; i++)
            {
                if (string.IsNullOrEmpty(character.EquippedSkillIds[i]))
                {
                    character.EquippedSkillIds[i] = skillId;

                    Debug.Log($"[SkillEquipService] Equipped Skill: {characterId} / {skillId} / Slot:{i}");
                    return true;
                }
            }

            Debug.LogWarning($"[SkillEquipService] Skill slot is full: {characterId}");
            return false;
        }

        public void UnequipSkill(string characterId, string skillId)
        {
            if (!characterStore.TryGet(characterId, out var character))
                return;

            if (character.EquippedSkillIds == null)
                return;

            for (int i = 0; i < character.EquippedSkillIds.Length; i++)
            {
                if (character.EquippedSkillIds[i] == skillId)
                {
                    character.EquippedSkillIds[i] = null;

                    Debug.Log($"[SkillEquipService] Unequipped Skill: {characterId} / {skillId} / Slot:{i}");
                    return;
                }
            }
        }
    }
}
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public enum SkillEquipSlotType
    {
        Move,
        Unique,
        Ability,
        Free1,
        Free2
    }

    public class SkillEquipService
    {
        private readonly CharacterRuntimeStore characterStore;

        public SkillEquipService(CharacterRuntimeStore characterStore)
        {
            this.characterStore = characterStore;
        }

        public bool EquipSkill(string characterId, SkillEquipSlotType slotType, string skillId)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                Debug.LogWarning("[SkillEquipService] CharacterId is empty.");
                return false;
            }

            if (!characterStore.TryGet(characterId, out var character))
            {
                Debug.LogWarning($"[SkillEquipService] Character runtime not found: {characterId}");
                return false;
            }

            EnsureEquippedSkillArray(character);

            switch (slotType)
            {
                case SkillEquipSlotType.Move:
                    character.MoveSkillId = skillId;
                    break;

                case SkillEquipSlotType.Unique:
                    character.UniqueSkillId = skillId;
                    character.EquippedSkillIds[0] = skillId;
                    break;

                case SkillEquipSlotType.Ability:
                    character.AbilitySkillId = skillId;
                    character.EquippedSkillIds[1] = skillId;
                    break;

                case SkillEquipSlotType.Free1:
                    character.EquippedSkillIds[2] = skillId;
                    break;

                case SkillEquipSlotType.Free2:
                    character.EquippedSkillIds[3] = skillId;
                    break;
            }

            Debug.Log($"[SkillEquipService] Equipped {slotType}: {characterId} / {skillId}");
            return true;
        }

        public bool UnequipSkill(string characterId, SkillEquipSlotType slotType)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                Debug.LogWarning("[SkillEquipService] CharacterId is empty.");
                return false;
            }

            if (!characterStore.TryGet(characterId, out var character))
            {
                Debug.LogWarning($"[SkillEquipService] Character runtime not found: {characterId}");
                return false;
            }

            EnsureEquippedSkillArray(character);

            switch (slotType)
            {
                case SkillEquipSlotType.Move:
                    Debug.LogWarning("[SkillEquipService] Move skill cannot be unequipped.");
                    return false;

                case SkillEquipSlotType.Unique:
                    Debug.LogWarning("[SkillEquipService] Unique skill cannot be unequipped.");
                    return false;

                case SkillEquipSlotType.Ability:
                    Debug.LogWarning("[SkillEquipService] Ability skill cannot be unequipped.");
                    return false;

                case SkillEquipSlotType.Free1:
                    character.EquippedSkillIds[2] = "";
                    break;

                case SkillEquipSlotType.Free2:
                    character.EquippedSkillIds[3] = "";
                    break;
            }

            Debug.Log($"[SkillEquipService] Unequipped {slotType}: {characterId}");
            return true;
        }

        private void EnsureEquippedSkillArray(CharacterRuntimeData character)
        {
            if (character == null)
                return;

            if (character.EquippedSkillIds == null || character.EquippedSkillIds.Length != 4)
                character.EquippedSkillIds = new string[4];

            if (string.IsNullOrWhiteSpace(character.EquippedSkillIds[0]))
                character.EquippedSkillIds[0] = character.UniqueSkillId;

            if (string.IsNullOrWhiteSpace(character.EquippedSkillIds[1]))
                character.EquippedSkillIds[1] = character.AbilitySkillId;
        }
    }
}
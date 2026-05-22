using UnityEngine;

namespace Relic.Gameplay.Data
{
    public enum SkillEquipSlotType
    {
        Move,
        Passive,
        Ability1,
        Ability2,
        Ability3,
        Unique
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
            if (!characterStore.TryGet(characterId, out var character))
            {
                Debug.LogWarning($"[SkillEquipService] Character runtime not found: {characterId}");
                return false;
            }

            switch (slotType)
            {
                case SkillEquipSlotType.Move:
                    character.MoveSkillId = skillId;
                    break;
                case SkillEquipSlotType.Passive:
                    character.PassiveSkillId = skillId;
                    break;
                case SkillEquipSlotType.Ability1:
                    character.AbilitySkillId1 = skillId;
                    break;
                case SkillEquipSlotType.Ability2:
                    character.AbilitySkillId2 = skillId;
                    break;
                case SkillEquipSlotType.Ability3:
                    character.AbilitySkillId3 = skillId;
                    break;
                case SkillEquipSlotType.Unique:
                    character.UniqueSkillId = skillId;
                    break;
            }

            Debug.Log($"[SkillEquipService] Equipped {slotType}: {characterId} / {skillId}");
            return true;
        }
    }
}
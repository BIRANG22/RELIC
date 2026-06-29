using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public class CharacterEquipmentManager
    {
        private readonly Dictionary<string, CharacterEquipmentData> equipmentMap = new();

        public CharacterEquipmentData GetOrCreate(string characterId)
        {
            if (!equipmentMap.TryGetValue(characterId, out var equipment))
            {
                equipment = new CharacterEquipmentData { CharacterId = characterId };
                EnsureArrays(equipment);
                equipmentMap[characterId] = equipment;
            }

            EnsureArrays(equipment);
            return equipment;
        }

        public void EquipPassive(string characterId, string passiveId)
        {
            GetOrCreate(characterId).PassiveSkillId = passiveId;
        }

        public void EquipUnique(string characterId, string uniqueId)
        {
            GetOrCreate(characterId).UniqueSkillId = uniqueId;
        }

        public void EquipAbility(string characterId, string abilityId)
        {
            GetOrCreate(characterId).AbilitySkillId = abilityId;
        }

        public void EquipFreeSkill(string characterId, int slotIndex, string skillId)
        {
            CharacterEquipmentData equipment = GetOrCreate(characterId);

            if (slotIndex < 0 || slotIndex >= equipment.FreeSkillIds.Length)
                return;

            equipment.FreeSkillIds[slotIndex] = skillId;
        }

        public void EquipRune(string characterId, int slotIndex, string runeId)
        {
            CharacterEquipmentData equipment = GetOrCreate(characterId);

            if (slotIndex < 0 || slotIndex >= equipment.RuneIds.Length)
                return;

            equipment.RuneIds[slotIndex] = runeId;
        }

        public void EquipFragment(string characterId, int slotIndex, string fragmentId)
        {
            CharacterEquipmentData equipment = GetOrCreate(characterId);

            if (slotIndex < 0 || slotIndex >= equipment.FragmentIds.Length)
                return;

            equipment.FragmentIds[slotIndex] = fragmentId;
        }

        private void EnsureArrays(CharacterEquipmentData equipment)
        {
            if (equipment == null)
                return;

            if (equipment.FreeSkillIds == null ||
                equipment.FreeSkillIds.Length != 2)
            {
                equipment.FreeSkillIds = new string[2];
            }

            if (equipment.RuneIds == null ||
                equipment.RuneIds.Length != 5)
            {
                equipment.RuneIds = new string[5];
            }

            if (equipment.FragmentIds == null ||
                equipment.FragmentIds.Length != 4)
            {
                equipment.FragmentIds = new string[4];
            }
        }
    }
}

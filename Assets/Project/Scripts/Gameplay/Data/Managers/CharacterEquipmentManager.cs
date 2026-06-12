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
                EnsureLoadout(equipment);
                equipmentMap[characterId] = equipment;
            }

            EnsureLoadout(equipment);
            return equipment;
        }

        public void EquipPassive(string characterId, string passiveId)
        {
            GetOrCreate(characterId).SkillLoadout.PassiveId = passiveId;
        }

        public void EquipUnique(string characterId, string uniqueId)
        {
            GetOrCreate(characterId).SkillLoadout.UniqueSkillId = uniqueId;
        }

        public void EquipAbility(string characterId, string abilityId)
        {
            GetOrCreate(characterId).SkillLoadout.AbilitySkillId = abilityId;
        }

        public void EquipFreeSkill(string characterId, int slotIndex, string skillId)
        {
            CharacterSkillLoadout loadout = GetOrCreate(characterId).SkillLoadout;

            if (slotIndex < 0 || slotIndex >= loadout.FreeSkillIds.Length)
                return;

            loadout.FreeSkillIds[slotIndex] = skillId;
        }

        public void EquipRune(string characterId, int slotIndex, string runeId)
        {
            CharacterEquipmentData equipment = GetOrCreate(characterId);

            if (slotIndex < 0 || slotIndex >= equipment.RuneLoadout.RuneIds.Length)
                return;

            equipment.RuneLoadout.RuneIds[slotIndex] = runeId;
        }

        public void EquipFragment(string characterId, int slotIndex, string fragmentId)
        {
            CharacterEquipmentData equipment = GetOrCreate(characterId);

            if (slotIndex < 0 || slotIndex >= equipment.FragmentIds.Length)
                return;

            equipment.FragmentIds[slotIndex] = fragmentId;
        }

        private void EnsureLoadout(CharacterEquipmentData equipment)
        {
            if (equipment == null)
                return;

            if (equipment.SkillLoadout == null)
                equipment.SkillLoadout = new CharacterSkillLoadout();

            if (equipment.SkillLoadout.FreeSkillIds == null ||
                equipment.SkillLoadout.FreeSkillIds.Length != 2)
            {
                equipment.SkillLoadout.FreeSkillIds = new string[2];
            }

            if (equipment.RuneLoadout == null)
                equipment.RuneLoadout = new CharacterRuneLoadout();

            if (equipment.RuneLoadout.RuneIds == null ||
                equipment.RuneLoadout.RuneIds.Length != 5)
            {
                equipment.RuneLoadout.RuneIds = new string[5];
            }

            if (equipment.FragmentIds == null ||
                equipment.FragmentIds.Length != 4)
            {
                equipment.FragmentIds = new string[4];
            }
        }
    }
}
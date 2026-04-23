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
                equipmentMap[characterId] = equipment;
            }
            return equipment;
        }

        public void EquipPassive(string characterId, string passiveId) => GetOrCreate(characterId).SkillLoadout.PassiveId = passiveId;
        public void EquipUnique(string characterId, string uniqueId) => GetOrCreate(characterId).SkillLoadout.UniqueSkillId = uniqueId;
        public void EquipCommon(string characterId, int slotIndex, string skillId) => GetOrCreate(characterId).SkillLoadout.CommonSkillIds[slotIndex] = skillId;
        public void EquipRune(string characterId, int slotIndex, string runeId) => GetOrCreate(characterId).RuneLoadout.RuneIds[slotIndex] = runeId;
        public void EquipFragment(string characterId, int slotIndex, string fragmentId) => GetOrCreate(characterId).FragmentIds[slotIndex] = fragmentId;
    }
}

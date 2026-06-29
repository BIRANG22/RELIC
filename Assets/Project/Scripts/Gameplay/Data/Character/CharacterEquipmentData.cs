using System;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class CharacterEquipmentData
    {
        public string CharacterId;
        public string PassiveSkillId;
        public string UniqueSkillId;
        public string AbilitySkillId;
        public string[] FreeSkillIds = new string[2];
        public string[] RuneIds = new string[5];
        public string[] FragmentIds = new string[4];
    }
}

using System;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class CharacterEquipmentData
    {
        public string CharacterId;
        public CharacterSkillLoadout SkillLoadout = new();
        public CharacterRuneLoadout RuneLoadout = new();
        public string[] FragmentIds = new string[6];
    }
}

using System;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class CharacterSkillLoadout
    {
        public string PassiveId;
        public string UniqueSkillId;
        public string AbilitySkillId;

        public string[] FreeSkillIds = new string[2];
    }
}

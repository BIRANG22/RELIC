using System;
using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class CharacterGrowthData
    {
        public string CharacterId;
        public bool IsUnlocked;
        public int CurrentLevel = 1;
        public int TotalExperience;
        public int RequiredExperience;
        public bool IsPassiveUnlocked;
        public bool IsUniqueUnlocked;
        public bool[] CommonSkillUnlocked = new bool[3];
        public bool[] CommonSkillShared = new bool[3];
        public List<string> ExclusiveRuneUnlockedIds = new();
    }
}

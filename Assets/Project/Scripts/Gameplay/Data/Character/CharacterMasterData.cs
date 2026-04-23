using System;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class CharacterMasterData
    {
        public string CharacterId;
        public string Name;
        public int MaxHealth;
        public int MaxStamina;
        public string ResourceType;
        public int MaxResource;
        public int MoveValue;
        public int MoveLevel;
        public bool IsDefaultProvided;
        public string UnlockCondition;
        public CharacterSkillLoadout DefaultSkillLoadout = new();
    }
}

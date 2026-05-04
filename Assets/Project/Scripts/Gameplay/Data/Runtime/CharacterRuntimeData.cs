using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    [System.Serializable]
    public class CharacterRuntimeData
    {
        public string CharacterId;

        public int Level = 1;
        public int Exp = 0;

        public int CurrentHealth;
        public int CurrentStamina;
        public int CurrentResource;
        public int CurrentMoveLevel;

        public string[] EquippedSkillIds = new string[4];
        public List<string> EquippedItemIds = new();

        public bool IsUnlocked;
    }
}
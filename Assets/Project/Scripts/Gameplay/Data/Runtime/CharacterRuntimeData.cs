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
        public int CurrentShield;

        public List<StatusEffectRuntimeData> StatusEffects = new();

        public string MoveSkillId;      // 기본 이동, 항상 있음
        public string PassiveSkillId;   // 장착 저장만, 전투 버튼에는 안 보임

        public string AbilitySkillId1;  // 전투 스킬 슬롯 1
        public string AbilitySkillId2;  // 전투 스킬 슬롯 2
        public string AbilitySkillId3;  // 전투 스킬 슬롯 3, 비워둘 수 있음

        public string UniqueSkillId;    // 전투 스킬 슬롯 4

        public string[] EquippedSkillIds = new string[4];
        public string[] EquippedRuneIds = new string[4];
        public List<string> EquippedItemIds = new();

        public bool IsUnlocked;
    }
}
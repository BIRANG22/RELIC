using System;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class CharacterMasterData
    {
        public string CharacterId;
        public string Name;

        public int MaxHealth;
        public int MaxStamina;
        public int StaminaRecovery;
        public int MaxResource;
        public string ResourceType;
        public int MoveValue;

        public bool IsDefaultProvided;
        public string UnlockCondition;

        // 엑셀 컬럼과 직접 매칭
        public string PassiveSkill1;
        public string PassiveSkill2;

        public string UniqueSkill1;
        public string UniqueSkill2;

        public string CharacterSkill1;
        public string CharacterSkill2;

        public string CommonSkill1;
        public string CommonSkill2;

        public string Rune1;
        public string Rune2;
        public string Rune3;
        public string Rune4;
        public string Rune5;

        // 런타임에서 쓰기 편한 구조
        public CharacterSkillLoadout DefaultSkillLoadout = new();

        [NonSerialized]
        public GameObject BattlePrefab;

        [NonSerialized]
        public Sprite Icon;

        public void BuildSkillLoadout()
        {
            DefaultSkillLoadout.PassiveId = PassiveSkill1;
            DefaultSkillLoadout.UniqueSkillId = UniqueSkill1;

            DefaultSkillLoadout.AbilitySkillIds = new string[3];
            DefaultSkillLoadout.AbilitySkillIds[0] = CharacterSkill1;
            DefaultSkillLoadout.AbilitySkillIds[1] = CharacterSkill2;
            DefaultSkillLoadout.AbilitySkillIds[2] = CommonSkill1;
        }

        public string[] GetRuneIds()
        {
            return new string[]
            {
                Rune1,
                Rune2,
                Rune3,
                Rune4,
                Rune5
            };
        }
    }
}
using System;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public enum ResourceType
    {
        None,
        Rage,
        Momentum,
        Aether
    }
    public enum ResourceTrigger
    {
        None,

        OnDamaged,              // 피격 시
        OnUseSameSlotTwice,     // 슬롯 하나에 특정 행동 2회
        OnSpendStaminaInSlot    // 슬롯 하나에 스태미나 일정량 소모
    }

    [Serializable]
    public class CharacterMasterData
    {
        public string CharacterId;
        public string Name;
        public string Introduction;

        public int MaxHealth;
        public int MaxStamina;
        public int StaminaRecovery;
        public int MaxResource;
        public ResourceType ResourceType;
        public ResourceTrigger ResourceTrigger;
        public int MoveValue;

        public bool IsDefaultProvided;
        public string UnlockCondition;

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

        public CharacterSkillLoadout DefaultSkillLoadout = new();

        [NonSerialized]
        public GameObject BattlePrefab;

        [NonSerialized]
        public Sprite Icon;

        public void BuildSkillLoadout()
        {
            if (DefaultSkillLoadout == null)
                DefaultSkillLoadout = new CharacterSkillLoadout();

            DefaultSkillLoadout.PassiveId = PassiveSkill1;
            DefaultSkillLoadout.UniqueSkillId = UniqueSkill1;
            DefaultSkillLoadout.AbilitySkillId = CharacterSkill1;

            DefaultSkillLoadout.FreeSkillIds = new string[2];
            DefaultSkillLoadout.FreeSkillIds[0] = CommonSkill1;
            DefaultSkillLoadout.FreeSkillIds[1] = "";
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

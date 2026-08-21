using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Relic.Gameplay.Data
{
    public enum ResourceType
    {
        None,
        Rage,
        Momentum,
        Aether,
        Faith,
        Blood
    }
    public enum ResourceTrigger
    {
        None,

        OnAnyAllyDamaged,          // 아군 또는 자신이 피해를 받았을 때
        OnThreeActionsInSameSlot,  // 한 슬롯에서 이동을 제외한 행동을 3회 했을 때
        OnSpendEightCostInTurn,    // 한 턴 동안 코스트를 8 이상 소모했을 때
        OnAllyBuffApplied,         // 아군 또는 자신이 이로운 효과를 받았을 때
        OnDamageEnemy              // 공격으로 적에게 피해를 주었을 때
    }

    [Serializable]
    public class CharacterMasterData
    {
        public string CharacterId;
        public string Name;
        public string Introduction;
        public string Regeneration;

        [FormerlySerializedAs("MaxHealth")]
        public int MaxHP;
        [FormerlySerializedAs("MaxStamina")]
        public int MaxCost;
        [FormerlySerializedAs("StaminaRecovery")]
        public int CostRecovery;
        public int MaxResource;
        public ResourceType ResourceType;
        public ResourceTrigger ResourceTrigger;

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
        [NonSerialized]
        public GameObject BattlePrefab;

        [NonSerialized]
        public Sprite Icon;

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

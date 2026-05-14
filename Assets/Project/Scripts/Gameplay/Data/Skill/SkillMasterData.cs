using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public enum SkillType
    {
        None,
        Power,
        Attack,
        Skill
    }

    public enum ActionType
    {
        None,
        Attack,
        Defense,
        Buff,
        Debuff,
        Heal,
        Move
    }

    public enum Category
    {
        Move,
        Passive,
        Unique,
        Ability,
        Essenece
    }

    public enum ReferenceResource
    {
        MovePoint,
        UniqueResource,
        Stamina,
        Health
    }

    public enum RangeType
    {
        None,
        Grid,
        Direction
    }

    public enum ResourceCostType
    {
        None,
        Fixed,
        AllCurrent
    }

    public enum PassiveFormulaType
    {
        None,
        Per1Resource_Stack1,
        Per2Resource_Stack2,
        FromMin_Per1Resource_Stack1
    }

    public enum TargetType
    {
        Self,
        PlayerParty,
        EnemyParty
    }

    public enum ValueCalcType
    {
        None,
        Fixed,   // 고정값
        PerCost  // 실제 소모량 * 값
    }

    [System.Serializable]
    public class ResourceCostData
    {
        public ResourceCostType ResourceCostType;
        public int ResourceCostValue;
    }

    [System.Serializable]
    public class SkillMasterData
    {
        public string SkillId;
        public string Name;
        public Category Category;
        public ReferenceResource ReferenceResource;
        public TargetType Target;
        public SkillType SkillType;

        // 엑셀 원본 문자열: 세미콜론 구분
        public string EffectIds;
        public string ValueCalcTypes;
        public string ValueRate;
        public string CountCalcTypes;
        public string CountRate;

        // 패시브
        public PassiveFormulaType PassiveFormulaType;
        public int PassiveMinResource;

        // 자원 소모
        public ResourceCostData ResourceCost;

        public int GridMove;
        public RangeType RangeType;
        public string RangeId;
        public ActionType TimelineNotation;

        // 런타임 사용용
        public List<SkillEffectEntry> EffectEntries = new();

        [System.NonSerialized]
        public Sprite Icon;
    }
}
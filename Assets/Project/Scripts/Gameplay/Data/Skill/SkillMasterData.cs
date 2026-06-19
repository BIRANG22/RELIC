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

    public enum TimelineActionType
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
        Move,       // 기본 이동
        Passive,    // 패시브
        Unique,     // 고유 스킬
        Ability,    // 캐릭터 전용 스킬
        Public,     // 공유 스킬
        Core        // 전투 획득 스킬
    }

    public enum ReferenceResource
    {
        MovePoint = 0,
        UniqueResource = 1,
        Stamina = 2,
        Health = 3,

        // SkillMaster의 ReferenceResource 컬럼에서 사용하는 별칭입니다.
        // Cost  -> 스태미나 / 코스트
        // Hp    -> 체력
        // Ulti  -> 고유자원
        Cost = Stamina,
        Hp = Health,
        Ulti = UniqueResource
    }

    public enum RangeType
    {
        None,
        Selection,
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
        ResourceStack,      // 자원 N마다 스택 M
        MinResourceStack    // 자원 N 이상이면 스택 M
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
    public class SkillMasterData
    {
        public string SkillId;
        public string Name;
        public Category Category;
        public ReferenceResource ReferenceResource;
        public TargetType Target;
        public SkillType SkillType;
        public TimelineActionType TimelineNotation;

        // 엑셀 원본 문자열: 세미콜론 구분
        public string EffectIds;
        public string ValueCalcTypes;
        public string ValueRate;
        public string CountRate;

        // 패시브
        public PassiveFormulaType PassiveFormulaType;
        public int PassiveMinResource;

        // 자원 소모
        public ResourceCostType ResourceCostType;
        public int ResourceCostValue;

        public int GridMove;
        public RangeType RangeType;
        public string RangeId;

        // CSV/엑셀에 EffectDescription 또는 EffectDesc 컬럼을 추가하면 자동으로 매핑됩니다.
        public string EffectDescription;
        public string EffectDesc;

        public string ToolTip;
        public string Details;

        // 런타임 사용용
        public List<SkillEffectEntry> EffectEntries = new();

        [System.NonSerialized]
        public Sprite Icon;
    }
}

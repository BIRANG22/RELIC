using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public enum SkillType
    {
        None,
        Buff,
        Debuff,
        Attack
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

    public enum SkillRarity
    {
        None = 0,
        Move = 1,

        // 현재 기억 레어도 체계
        Exclusive = 2,
        Common = 3,
        Rare = 4,
        Epic = 5,
        Unique = 6,

        // 이전 코드 호환용 별칭
        Passive = Exclusive,
        CharacterExclusive = Exclusive,
        Shared = Common,
        CoreCommon = Common,
        CoreRare = Rare,
        CoreEpic = Epic
    }

    public enum ReferenceResource
    {
        MovePoint = 0,
        UniqueResource = 1,
        Cost = 2,
        HP = 3,

        // Legacy Excel aliases.
        Stamina = Cost,
        Health = HP,
        Hp = HP,
        Ulti = UniqueResource
    }

    public enum RangeType
    {
        None,
        Selection,
        Direction,
        Passive
    }

    public enum TargetType
    {
        Self,
        PlayerParty,
        EnemyParty
    }

    [System.Serializable]
    public class SkillMasterData
    {
        public string SkillId;
        public string Name;
        public Category Category;
        public ReferenceResource ReferenceResource;
        public TargetType Target;
        public int Level;
        public SkillRarity Rarity;
        public SkillType SkillType;
        public TimelineActionType TimelineNotation;

        // 엑셀 원본 문자열: 세미콜론 구분
        public string EffectIds;
        public string ValueRate;
        public string CountRate;

        // 스킬이 사용하는 자원의 고정 소모량
        public int ResourceCostValue;

        public int GridMove;
        public RangeType RangeType;
        public string RangeId;

        public string Details;

        // 런타임 사용용
        public List<SkillEffectEntry> EffectEntries = new();

        [System.NonSerialized]
        public Sprite Icon;
    }
}

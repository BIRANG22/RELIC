using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public enum SkillType
    {
        Power,
        Attack,
        Skill
    }

    public enum ActionType
    {
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

    [System.Serializable]
    public class SkillMasterData
    {
        public string SkillId;
        public string Name;
        public Category Category;
        public ReferenceResource ReferenceResource;
        public string Target;
        public SkillType SkillType;
        public string Value_Formula;
        public ActionType ActionType;
        public string Description;

        public int Consumption;

        public float CoolTime;
        public int Cost;
        public int Power;
        public string RangeId;

        public List<string> EffectIds = new();
        public List<SkillEffectData> Effects = new();

        public string PassiveTrigger;
        public string UniqueOwnerId;
        public int EssenceGrade;

        [System.NonSerialized]
        public Sprite Icon;
    }
}
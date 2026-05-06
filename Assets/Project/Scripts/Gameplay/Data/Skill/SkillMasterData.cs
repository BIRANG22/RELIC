using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public enum SkillType
    {
        Passive,
        Unique,
        Common,
        Essence
    }

    [System.Serializable]
    public class SkillMasterData
    {
        public string SkillId;
        public SkillType SkillType;
        public string Name;
        public string Description;

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
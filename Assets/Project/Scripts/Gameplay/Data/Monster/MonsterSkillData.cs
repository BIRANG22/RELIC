using System;
using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class MonsterSkillData
    {
        public string SkillId;
        public string Name;

        public TargetType Target;

        public string EffectIds;
        public string ValueCalcTypes;
        public string ValueRate;
        public string CountCalcTypes;
        public string CountRate;

        public int GridMove;
        public RangeType RangeType;
        public string RangeId;

        public ActionType TimelineNotation;

        public List<SkillEffectEntry> EffectEntries = new();
    }
}
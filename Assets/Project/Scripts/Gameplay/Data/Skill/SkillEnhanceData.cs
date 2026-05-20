using System;
using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class SkillEnhanceData
    {
        public string Name;
        public int EnhancementLevel;
        public string NameSuffix;

        public TargetType Target;
        public SkillType SkillType;

        public string EffectIds;
        public string ValueCalcTypes;
        public string ValueRate;
        public string CountCalcTypes;
        public string CountRate;

        public int Consumption;

        public int GridMove;
        public RangeType RangeType;
        public string RangeId;

        public List<SkillEffectEntry> EffectEntries = new();
    }
}
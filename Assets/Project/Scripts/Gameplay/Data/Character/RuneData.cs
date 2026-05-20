using System;
using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class RuneData
    {
        public string RuneId;
        public string Name;

        public string TargetCharacterId;
        public string TargetSkillId;

        public string EffectIds;
        public string ValueCalcTypes;
        public string ValueRate;
        public string CountCalcTypes;
        public string CountRate;

        public int Consumption;

        public int EnhancementLevel;

        public RangeType RangeType;
        public string RangeId;

        public List<SkillEffectEntry> EffectEntries = new();
    }
}
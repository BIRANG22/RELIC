using System;
using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class UniqueSkillData
    {
        public string SkillId;
        public string Name;
        public int MinUseAmount;
        public int MaxUseAmount;
        public int CostValue;
        public string TargetType;
        public string RangeId;
        public List<SkillEffectData> Effects = new();
    }
}

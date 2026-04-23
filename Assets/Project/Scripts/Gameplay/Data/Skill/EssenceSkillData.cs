using System;
using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class EssenceSkillData
    {
        public string SkillId;
        public string Name;
        public string Source;
        public string SkillType;
        public string Grade;
        public string CostResource;
        public int CostValue;
        public string TargetType;
        public string RangeId;
        public List<SkillEffectData> Effects = new();
    }
}

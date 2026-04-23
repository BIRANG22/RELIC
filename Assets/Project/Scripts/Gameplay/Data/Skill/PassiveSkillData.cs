using System;
using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class PassiveSkillData
    {
        public string SkillId;
        public string Name;
        public string UniqueResourceType;
        public string ResourceGainCondition;
        public string ActivationCondition;
        public List<SkillEffectData> Effects = new();
    }
}

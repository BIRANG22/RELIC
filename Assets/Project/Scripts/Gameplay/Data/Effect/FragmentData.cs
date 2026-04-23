using System;
using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class FragmentData
    {
        public string FragmentId;
        public string Name;
        public string Type;
        public string EquipConditionType;
        public string EquipConditionValue;
        public List<SkillEffectData> Effects = new();
    }
}

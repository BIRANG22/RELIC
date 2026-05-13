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

        // 엑셀 원본 (세미콜론)
        public string EffectIds;
        public string ValueCalcTypes;
        public string ValueAmounts;
        public string CountCalcTypes;
        public string CountAmounts;

        // 런타임용
        public List<SkillEffectEntry> EffectEntries = new();
    }
}
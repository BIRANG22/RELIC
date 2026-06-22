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

        public string RangeId;

        public TimelineActionType TimelineNotation;

        // CSV/엑셀에 EffectDescription 또는 EffectDesc 컬럼을 추가하면 자동으로 매핑됩니다.
        public string EffectDesc;

        public List<SkillEffectEntry> EffectEntries = new();
    }
}

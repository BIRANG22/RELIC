using System;
using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class CommonSkillData
    {
        public string SkillId;
        public string Name;
        public string OwnerCharacterId;
        public string SkillType;
        public int UnlockStage;
        public int ShareStage;
        public string CostResource;
        public int CostValue;
        public string TargetType;
        public string RangeId;
        public List<SkillEffectData> Effects = new();
        public string AdditionalEffectCondition;
    }
}

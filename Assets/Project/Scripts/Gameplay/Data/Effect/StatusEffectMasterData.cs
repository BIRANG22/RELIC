using System;
using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class StatusEffectMasterData
    {
        public string StatusEffectId;
        public string Name;
        public string Category;
        public string ProcessType;
        public string TargetType;
        public bool IsStackable;
        public int MaxStackCount;
        public string TriggerTiming;
        public string ValueType;
        public bool IgnoreArmor;
        public bool IsDispellable;
        public string DispelCategory;
        public List<SkillEffectData> Effects = new();
    }
}

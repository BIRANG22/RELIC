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
        public string ValueRate;
        public string CountRate;

        public int BlueDustiumCost;
        public int UnlockLevel;

        public string EffectDesc;
        public string Rarity;

        public List<SkillEffectEntry> EffectEntries = new();
    }
}

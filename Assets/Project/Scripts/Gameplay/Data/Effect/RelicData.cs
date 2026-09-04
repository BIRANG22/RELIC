using System;
using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class RelicData
    {
        public string FragmentId;
        public string Name;
        public string Rarity;
        public int BlueDustiumCost;
        public int RedDustiumCost;

        public string EffectIds;
        public string ValueRate;
        public string CountRate;


        public string EffectDesc;

        public List<SkillEffectEntry> EffectEntries = new();
    }
}
